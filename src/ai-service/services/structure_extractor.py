"""
Structure extraction from Syrian textbook Table of Contents.
Uses OCR-based TocParser first, falls back to AI providers in priority order for vision-based extraction.
"""

import json
from typing import Optional
from models.dto import DocumentStructure, TocEntry


class StructureExtractor:
    """
    Extracts book structure (chapters/sections) from the Table of Contents page.
    
    Strategy:
    1. Try OCR-based TocParser on extracted text (fast, free, no API cost)
    2. If that fails, render the TOC page as image and try each AI provider
       in the configured priority order (e.g. groq -> gemini -> openrouter)
    3. Build a page_number -> (chapter, section) mapping for tagging during extraction
    """
    
    def __init__(self, pdf_service, gemini_service, groq_service, openrouter_service,
                 toc_parser, logger, config):
        self.pdf_service = pdf_service
        self.gemini_service = gemini_service
        self.groq_service = groq_service
        self.openrouter_service = openrouter_service
        self.toc_parser = toc_parser
        self.logger = logger
        self.config = config
    
    def _get_vision_providers(self):
        """
        Return ordered list of (provider_name, callable) for TOC vision extraction,
        based on the configured PROVIDER_PRIORITY.
        Each callable has signature: (prompt: str, image_bytes: bytes) -> str | None
        """
        providers = []
        for provider_name in getattr(self.config, "PROVIDER_PRIORITY", ["groq", "gemini", "openrouter"]):
            provider_name = provider_name.strip().lower()
            if provider_name == "gemini":
                providers.append(("gemini", self.gemini_service.call_with_prompt_and_image))
            elif provider_name == "groq":
                providers.append(("groq", self.groq_service.call_with_prompt_and_image))
            elif provider_name == "openrouter":
                providers.append(("openrouter", self.openrouter_service.call_with_prompt_and_image))
        return providers
    
    def extract_structure(self, pdf_path: str, total_pages: int, toc_page: int) -> Optional[DocumentStructure]:
        """
        Main entry point. Extract structure from a known TOC page.
        
        Args:
            pdf_path: Path to the PDF file
            total_pages: Total number of pages in the document
            toc_page_num: The page number (1-indexed) of the Table of Contents
        """
        self.logger.info(f"Extracting structure from TOC page {toc_page}")
        
        # Step 1: Try OCR-based extraction first (free, no API cost)
        structure = self._try_ocr_extraction(pdf_path, toc_page)
        
        if structure and structure.total_entries >= 2:
            structure.extraction_method = "toc_parser"
            self.logger.info(f"OCR extraction succeeded: {structure.total_entries} entries")
            return structure
        
        # Step 2: Fall back to vision-based extraction using providers in priority order
        self.logger.info("OCR extraction insufficient, falling back to AI vision providers")
        structure = self._try_vision_extraction(pdf_path, toc_page)
        
        if structure and structure.total_entries >= 2:
            structure.extraction_method = "ai_fallback"
            self.logger.info(f"Vision extraction succeeded with {structure.extraction_method}: {structure.total_entries} entries")
            return structure
        
        self.logger.warning("All structure extraction methods failed")
        return None
    
    def _try_ocr_extraction(self, pdf_path: str, toc_page_num: int) -> Optional[DocumentStructure]:
        """Extract structure using OCR text + TocParser."""
        doc = None
        try:
            import fitz
            doc = fitz.open(pdf_path)
            
            if toc_page_num < 1 or toc_page_num > len(doc):
                self.logger.error(f"TOC page {toc_page_num} is out of range (1-{len(doc)})")
                return None
            
            page = doc.load_page(toc_page_num - 1)
            text = page.get_text()
            doc.close()
            doc = None
            
            self.logger.info(f"RAW TEXT FROM TOC PAGE {toc_page_num} ({len(text)} chars): {text[:800]}")
            
            # Check if TocParser recognizes this as TOC
            is_toc = self.toc_parser.is_toc_page(text)
            self.logger.info(f"TocParser.is_toc_page() returned: {is_toc}")
            
            # Parse the TOC text
            entries = self.toc_parser.parse_toc_page(text)
            self.logger.info(f"TocParser.parse_toc_page() found {len(entries)} entries")
            
            if not entries:
                self.logger.warning("TocParser found 0 entries - page may not be a Table of Contents")
                return None
            
            # Classify entries as chapters vs sections
            entries = self.toc_parser.identify_chapters_and_sections(entries)
            
            # Build the DocumentStructure
            chapters = [e for e in entries if e.level == "Chapter"]
            sections = [e for e in entries if e.level == "Section"]
            
            self.logger.info(f"Classified: {len(chapters)} chapters, {len(sections)} sections")
            
            return DocumentStructure(
                chapters=chapters,
                sections=sections,
                total_entries=len(entries),
                extraction_method="toc_parser"
            )
            
        except Exception as e:
            self.logger.error(f"OCR extraction failed: {e}")
            return None
        finally:
            if doc:
                doc.close()
    
    def _try_vision_extraction(self, pdf_path: str, toc_page_num: int) -> Optional[DocumentStructure]:
        """
        Try vision-based TOC extraction using AI providers in priority order.
        Each provider's response is parsed and validated; only returns a structure
        with >= 3 entries.
        """
        image_bytes = self.pdf_service.render_page_as_image(pdf_path, toc_page_num)
        if not image_bytes:
            self.logger.error("Failed to render TOC page as image")
            return None
        
        self.logger.info(f"Rendered page {toc_page_num} as image ({len(image_bytes)} bytes)")
        prompt = self._build_toc_vision_prompt()
        
        providers = self._get_vision_providers()
        self.logger.info(f"Vision providers to try (in order): {[p[0] for p in providers]}")
        
        for provider_name, call_fn in providers:
            self.logger.info(f"Attempting TOC vision extraction with {provider_name}")
            try:
                response_text = call_fn(prompt, image_bytes)
                if not response_text:
                    self.logger.warning(f"{provider_name} returned empty response for TOC vision")
                    continue
                
                structure = self._parse_vision_response(response_text, provider_name)
                if structure and structure.total_entries >= 2:
                    self.logger.info(
                        f"TOC vision extraction succeeded with {provider_name}: "
                        f"{structure.total_entries} entries"
                    )
                    return structure
                else:
                    self.logger.warning(
                        f"{provider_name} returned only {structure.total_entries if structure else 0} entries"
                    )
            except Exception as e:
                self.logger.warning(f"{provider_name} TOC vision extraction failed: {e}")
                continue
        
        self.logger.warning("All vision providers failed to extract a valid TOC structure")
        return None
    
    def _parse_vision_response(self, response_text: str, provider_name: str) -> Optional[DocumentStructure]:
        """Parse JSON from a vision provider response into a DocumentStructure."""
        try:
            # Try to extract JSON from the response (handles markdown-wrapped JSON)
            text = response_text.strip()
            if text.startswith("```"):
                # Extract from markdown code block
                import re
                match = re.search(r"```(?:json)?\s*\n?(.*?)\n?\s*```", text, re.DOTALL)
                if match:
                    text = match.group(1).strip()
            
            data = json.loads(text)
            self.logger.info(f"{provider_name} vision response: {json.dumps(data, ensure_ascii=False)[:500]}")
            
            chapters = []
            sections = []
            
            for item in data.get("entries", []):
                page_number = item.get("page_number")
                if page_number is None:
                    # AI models often return null for chapter headers that span
                    # multiple pages. Skip these entries but log a warning.
                    self.logger.warning(
                        f"Entry '{item.get('title', '')}' has null page_number — skipping"
                    )
                    continue
                
                entry = TocEntry(
                    title=item.get("title", ""),
                    page_number=page_number,
                    level=item.get("level", "Section"),
                    parent_chapter=item.get("parent_chapter")
                )
                
                if entry.level == "Chapter":
                    chapters.append(entry)
                else:
                    sections.append(entry)
            
            return DocumentStructure(
                chapters=chapters,
                sections=sections,
                total_entries=len(chapters) + len(sections),
                extraction_method=f"ai_fallback_{provider_name}"
            )
            
        except (json.JSONDecodeError, KeyError, ValueError) as e:
            self.logger.error(f"Failed to parse {provider_name} vision response: {e}")
            self.logger.error(f"Raw response: {response_text[:500]}")
            return None
    
    def _build_toc_vision_prompt(self) -> str:
        """Build a specialized prompt for vision models to extract TOC structure."""
        return """
        You are analyzing a scanned image of a Syrian school textbook's Table of Contents page (الفهرس).
        Extract ALL entries into a structured JSON format.

        Instructions:
        1. Identify each entry as either "Chapter" (الفصل/الوحدة/الباب) or "Section" (الدرس/الموضوع).
        2. For each entry, extract the exact title in Arabic and its page number.
        3. If a section belongs to a chapter, set the parent_chapter field with the chapter title.
        4. If the image is not a Table of Contents, return an empty entries array.

        Response format (JSON):
        {
        "entries": [
            {
            "title": "الوحدة الأولى: الأعداد والعمليات",
            "page_number": 5,
            "level": "Chapter",
            "parent_chapter": null
            },
            {
            "title": "الدرس الأول: الأعداد الطبيعية",
            "page_number": 7,
            "level": "Section",
            "parent_chapter": "الوحدة الأولى: الأعداد والعمليات"
            }
        ]
        }

        Important:
        - Extract EVERY entry visible on the page, even if partially cut off.
        - Page numbers must be integers.
        - Titles must be in Arabic exactly as they appear.
        - If the page contains only decoration or a chapter title page (not a TOC), return {"entries": []}.
        """
    
    def build_page_section_mapping(self, structure: DocumentStructure, total_pages: int) -> dict:
        """
        Build a lookup dictionary: page_number -> {chapter_title, section_title}

        Ensures every page gets a chapter AND section assignment, even pages that
        fall in the "gap" between a chapter header and its first section.

        Strategy:
            1. Pre-compute: for each chapter, find its first section via parent_chapter links.
            2. Walk entries in page order, assigning ranges (chapters reset section to None).
            3. Forward-fill: any page with a chapter but null section gets the chapter's
            first section title (or the chapter title itself if the chapter has no sections).
            4. Pre-first-entry pages: assign the first chapter/section context.
        """
        if not structure or structure.total_entries == 0 or total_pages <= 0:
            return {}

        # ── Pre-compute: chapter_title -> first_section_title ──
        chapter_first_section: dict[str, str] = {}
        for section in structure.sections:
            parent = section.parent_chapter
            if parent and parent not in chapter_first_section:
                chapter_first_section[parent] = section.title

        # Data quality signal: sections without a parent chapter link
        orphan_count = sum(1 for s in structure.sections if not s.parent_chapter)
        if orphan_count:
            self.logger.warning(
                f"{orphan_count} section(s) have no parent_chapter — "
                "they will still be mapped by page order but forward-fill "
                "cannot link them to a chapter by name."
            )

        # ── Sort all entries by page number ──
        all_entries = sorted(
            structure.chapters + structure.sections,
            key=lambda e: e.page_number,
        )

        if not all_entries:
            self.logger.warning("No TOC entries found after sorting — cannot build page mapping")
            return {}

        mapping: dict[int, dict[str, str | None]] = {}
        current_chapter: str | None = None
        current_section: str | None = None

        # ── Pass 1: Assign raw ranges from TOC entries ──
        for i, entry in enumerate(all_entries):
            start = entry.page_number
            end = (
                all_entries[i + 1].page_number - 1
                if i + 1 < len(all_entries)
                else total_pages
            )
            end = max(start, min(end, total_pages))

            if entry.level == "Chapter":
                current_chapter = entry.title
                current_section = None          # gap — fixed in pass 2
            else:
                current_section = entry.title

            for page in range(start, end + 1):
                mapping[page] = {
                    "chapter_title": current_chapter,
                    "section_title": current_section,
                }

        # ── Pass 2: Forward-fill null section_title within each chapter ──
        unfilled = 0
        for page_data in mapping.values():
            if page_data["chapter_title"] and not page_data["section_title"]:
                chapter = page_data["chapter_title"]
                page_data["section_title"] = chapter_first_section.get(chapter, chapter)
                unfilled += 1

        if unfilled:
            self.logger.info(f"Forward-filled section_title for {unfilled} page(s) in chapter gaps")

        # ── Pass 3: Fill pages that fall BEFORE the first TOC entry ──
        first_entry_page = all_entries[0].page_number

        first_chapter = next(
            (e.title for e in all_entries if e.level == "Chapter"),
            all_entries[0].parent_chapter if all_entries[0].level == "Section" else None,
        )

        if first_chapter:
            first_section = chapter_first_section.get(first_chapter) or first_chapter
        elif structure.sections:
            first_section = structure.sections[0].title
        else:
            first_section = first_chapter

        pre_first_count = 0
        for page in range(1, first_entry_page):
            if page not in mapping:
                mapping[page] = {
                    "chapter_title": first_chapter,
                    "section_title": first_section,
                }
                pre_first_count += 1

        if pre_first_count:
            self.logger.info(
                f"Assigned leading context to {pre_first_count} page(s) before first TOC entry (page {first_entry_page})"
            )

        self.logger.info(
            f"Final mapping: {len(mapping)}/{total_pages} pages covered, "
            f"{len(chapter_first_section)} chapters with linked sections"
        )

        return mapping
