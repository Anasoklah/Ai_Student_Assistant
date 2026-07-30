"""
Structure extraction from Syrian textbook Table of Contents.
Uses OCR-based TocParser first, falls back to AI providers in priority order for vision-based extraction.
"""

import json
from typing import Optional
from models.dto import DocumentStructure, TocEntry
from services.provider_roles import provider_allows

# FLOW OF FUNCTIONS:
#
# extract_structure (Public) -> Main entry point for book structure extraction.
#   ├── _try_ocr_extraction (Private) -> Strategy 1: OCR on text.
#   └── _try_vision_extraction_range (Private) -> Strategy 2: AI Vision fallback.
#       └── _try_vision_extraction (Private)
#           ├── _get_vision_providers (Private)
#           ├── _build_toc_vision_prompt (Private)
#           └── _parse_vision_response (Private)

class StructureExtractor:
    """
    Extracts book structure (chapters/sections) from the Table of Contents page.
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

    # --- PUBLIC METHODS ---
    
    def extract_structure(self, pdf_path: str, total_pages: int, toc_page: int, toc_page_end: int | None = None) -> Optional[DocumentStructure]:
        """
        Public entry point to extract the document structure.
        Tries OCR first, then AI vision as a fallback.
        """
        end_page = toc_page_end or toc_page
        self.logger.info(f"Extracting structure from TOC pages {toc_page}-{end_page}")

        # Step 1: OCR path
        structure = self._try_ocr_extraction(pdf_path, toc_page, end_page)
        if structure and structure.total_entries >= 2:
            structure.extraction_method = "toc_parser"
            self.logger.info(f"OCR extraction succeeded: {structure.total_entries} entries")
            return structure

        # Step 2: Vision fallback
        self.logger.info("OCR extraction insufficient, falling back to AI vision providers")
        structure = self._try_vision_extraction_range(pdf_path, toc_page, end_page)
        if structure and structure.total_entries >= 2:
            structure.extraction_method = "ai_fallback"
            self.logger.info(f"Vision extraction succeeded: {structure.total_entries} entries")
            return structure

        self.logger.warning("All structure extraction methods failed")
        return None

    # --- PRIVATE METHODS ---

    def _get_vision_providers(self):
        """
        Return ordered list of (provider_name, callable) for TOC vision extraction.
        """
        providers = []
        for provider_name in getattr(self.config, "PROVIDER_PRIORITY", ["groq", "gemini", "openrouter"]):
            provider_name = provider_name.strip().lower()
            if not provider_allows(self.config, provider_name, is_vision=True):
                continue
            if provider_name == "gemini":
                providers.append(("gemini", self.gemini_service.call_with_prompt_and_image))
            elif provider_name == "groq":
                providers.append(("groq", self.groq_service.call_with_prompt_and_image))
            elif provider_name == "openrouter":
                providers.append(("openrouter", self.openrouter_service.call_with_prompt_and_image))
        return providers

    def _try_ocr_extraction(self, pdf_path: str, toc_page_start: int, toc_page_end: int) -> Optional[DocumentStructure]:
        """
        Attempts to extract TOC entries using OCR'd text.
        """
        try:
            import fitz
            doc = fitz.open(pdf_path)
            try:
                if toc_page_start < 1 or toc_page_end > len(doc):
                    self.logger.error(f"TOC range {toc_page_start}-{toc_page_end} out of bounds (1-{len(doc)})")
                    return None

                combined_text = ""
                for page_num in range(toc_page_start, toc_page_end + 1):
                    page = doc.load_page(page_num - 1)
                    combined_text += page.get_text() + "\n"
            finally:
                doc.close()

            self.logger.info(f"RAW TEXT FROM TOC PAGES {toc_page_start}-{toc_page_end} ({len(combined_text)} chars): {combined_text[:800]}")

            entries = self.toc_parser.parse_toc_page(combined_text)
            self.logger.info(f"TocParser found {len(entries)} entries across {toc_page_end - toc_page_start + 1} page(s)")
            if not entries:
                return None

            entries = self.toc_parser.identify_chapters_and_sections(entries)
            chapters = [e for e in entries if e.level == "Chapter"]
            sections = [e for e in entries if e.level == "Section"]

            return DocumentStructure(
                chapters=chapters, sections=sections,
                total_entries=len(entries), extraction_method="toc_parser",
            )
        except Exception as e:
            self.logger.error(f"OCR extraction failed: {e}")
            return None

    def _try_vision_extraction_range(self, pdf_path: str, toc_page_start: int, toc_page_end: int) -> Optional[DocumentStructure]:
        """
        Vision fallback per page (each page is a separate image call), merged + deduped.
        """
        all_chapters, all_sections = [], []
        seen = set()  # (title, page_number) dedupe key

        for page_num in range(toc_page_start, toc_page_end + 1):
            structure = self._try_vision_extraction(pdf_path, page_num)
            if not structure:
                continue
            for entry in structure.chapters + structure.sections:
                key = (entry.title.strip(), entry.page_number)
                if key in seen:
                    continue
                seen.add(key)
                (all_chapters if entry.level == "Chapter" else all_sections).append(entry)

        total = len(all_chapters) + len(all_sections)
        if total == 0:
            return None

        return DocumentStructure(
            chapters=all_chapters, sections=all_sections,
            total_entries=total, extraction_method="ai_fallback",
        )

    def _try_vision_extraction(self, pdf_path: str, toc_page_num: int) -> Optional[DocumentStructure]:
        """
        Try vision-based TOC extraction for a single page using AI providers.
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
            text = response_text.strip()
            if text.startswith("```"):
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
