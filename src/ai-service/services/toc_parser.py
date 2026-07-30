"""
Table of Contents Parser for OCR'd Syrian Textbooks.
Handles the messy reality of OCR-extracted text from scanned books.
"""

import re
from typing import List, Optional
from models.dto import TocEntry

# FLOW OF FUNCTIONS:
#
# normalize_arabic_digits (Public) -> Converts Arabic-Indic digits to standard digits.
#
# is_toc_page (Public) -> Checks if a page text represents a Table of Contents.
#
# parse_toc_page (Public) -> Main entry point for parsing TOC entries from text.
#   ├── _remove_ocr_noise (Private)
#   ├── _try_standard_format (Private)
#   ├── _try_merged_format (Private)
#   └── _try_last_number_format (Private)
#
# identify_chapters_and_sections (Public) -> Classifies entries as 'Chapter' or 'Section'.

class TocParser:
    """
    Parses Table of Contents from OCR'd Syrian textbooks.
    """
    
    def __init__(self, logger):
        self.logger = logger
        
        # Arabic-Indic digits (٠١٢٣٤٥٦٧٨٩) to standard digits (0123456789)
        self.arabic_digit_map = {
            '٠': '0', '١': '1', '٢': '2', '٣': '3', '٤': '4',
            '٥': '5', '٦': '6', '٧': '7', '٨': '8', '٩': '9'
        }

    # --- PUBLIC METHODS ---
    
    def normalize_arabic_digits(self, text: str) -> str:
        """
        Convert Arabic-Indic digits to standard digits.
        Example: "٢٠" -> "20"
        """
        result = []
        for char in text:
            if char in self.arabic_digit_map:
                result.append(self.arabic_digit_map[char])
            else:
                result.append(char)
        return ''.join(result)
    
    def is_toc_page(self, text: str) -> bool:
        """
        Check if a page is the Table of Contents.
        Uses multiple signals because the word "فهرس" might be garbled in OCR output.
        """
        # Normalize digits first
        normalized = self.normalize_arabic_digits(text)
        
        # Signal 1: Keywords (with OCR-tolerant variants)
        toc_keywords = [
            'فهرس', 'الفهرس', 'المحتويات', 'فهرس المحتويات',
            'ف ھ ر س',  # OCR might split characters
            'فھرس',      # OCR might join characters
        ]
        has_keyword = any(kw in text for kw in toc_keywords)
        
        # Signal 2: High density of lines ending with numbers (ToC characteristic)
        lines = normalized.split('\n')
        number_line_count = 0
        total_lines = 0
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
            total_lines += 1
            
            # Check if line ends with a number (1-4 digits)
            if re.search(r'\d{1,4}\s*$', line) and len(line) > 3:
                number_line_count += 1
        
        # Signal 3: Ratio of numbered lines
        if total_lines > 0:
            ratio = number_line_count / total_lines
            has_high_number_ratio = ratio > 0.3
        else:
            has_high_number_ratio = False
        
        result = has_keyword or (number_line_count >= 5 and has_high_number_ratio)
        
        if result:
            self.logger.info(
                f"ToC detected: keyword={has_keyword}, "
                f"numbered_lines={number_line_count}/{total_lines}"
            )
        
        return result
    
    def parse_toc_page(self, text: str) -> List[TocEntry]:
        """
        Parse OCR'd Table of Contents.
        Tries multiple strategies suitable for OCR-mangled text.
        """
        # Normalize digits first
        text = self.normalize_arabic_digits(text)
        
        # Remove header/footer noise (common in OCR'd pages)
        text = self._remove_ocr_noise(text)
        
        lines = text.strip().split('\n')
        
        # Strategy 1: Try with standard spacing
        entries = self._try_standard_format(lines)
        if entries:
            self.logger.info(f"Parsed {len(entries)} entries with standard format")
            return entries
        
        # Strategy 2: Try with minimal spacing (OCR merged words and numbers)
        entries = self._try_merged_format(lines)
        if entries:
            self.logger.info(f"Parsed {len(entries)} entries with merged format")
            return entries
        
        # Strategy 3: Try with last-number extraction (finds page numbers anywhere)
        entries = self._try_last_number_format(lines)
        if entries:
            self.logger.info(f"Parsed {len(entries)} entries with last-number format")
            return entries
        
        self.logger.warning("All parsing strategies failed for OCR'd ToC")
        return []

    def identify_chapters_and_sections(self, entries: List[TocEntry]) -> List[TocEntry]:
        """
        After extraction, classify entries as Chapter or Section based on Syrian textbook patterns.
        """
        for entry in entries:
            title_lower = entry.title.lower()
            
            if any(word in title_lower for word in ['الوحدة', 'الفصل', 'الباب']):
                entry.level = "Chapter"
            elif 'الدرس' in title_lower:
                entry.level = "Section"
            else:
                # If it has a number at the start and is shorter, it's likely a chapter
                if re.match(r'^(ال)?[أ-ي].*\s+\d+', entry.title) and len(entry.title) < 30:
                    entry.level = "Chapter"
        
        return entries

    # --- PRIVATE METHODS ---

    def _remove_ocr_noise(self, text: str) -> str:
        """
        Remove common OCR noise patterns from page text.
        """
        lines = text.split('\n')
        cleaned = []
        
        for line in lines:
            line = line.strip()
            
            if not line:
                continue
            
            if re.match(r'^[\.\-_=]{3,}$', line):
                continue
            
            if len(line) < 3:
                continue
            
            if re.match(r'^[^ء-ي\w]{3,}$', line):
                continue
            
            cleaned.append(line)
        
        return '\n'.join(cleaned)
    
    def _try_standard_format(self, lines: List[str]) -> List[TocEntry]:
        """
        Standard format: Title followed by spaces then page number.
        """
        entries = []
        pattern = re.compile(r'^(.+?)\s{2,}(\d{1,4})\s*$')
        
        for line in lines:
            match = pattern.match(line)
            if match:
                title = match.group(1).strip()
                title = re.sub(r'[.]+\s*$', '', title)
                page = int(match.group(2))
                
                if page <= 2000:
                    entries.append(TocEntry(
                        title=title,
                        page_number=page,
                        level="Section"
                    ))
        
        return entries if len(entries) >= 3 else []
    
    def _try_merged_format(self, lines: List[str]) -> List[TocEntry]:
        """
        Handle OCR-merged text where spaces between title and number are lost.
        """
        entries = []
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
            
            match = re.search(r'^(.+?)(\d{1,4})\s*$', line)
            if match:
                potential_title = match.group(1).strip()
                page = int(match.group(2))
                
                has_arabic = bool(re.search(r'[\u0600-\u06FF]', potential_title))
                is_reasonable_page = 1 <= page <= 2000
                is_long_enough = len(potential_title) >= 3
                
                if has_arabic and is_reasonable_page and is_long_enough:
                    title = re.sub(r'[\.\-\s]+$', '', potential_title)
                    
                    entries.append(TocEntry(
                        title=title,
                        page_number=page,
                        level="Section"
                    ))
        
        return entries if len(entries) >= 3 else []
    
    def _try_last_number_format(self, lines: List[str]) -> List[TocEntry]:
        """
        Most robust strategy: Extract the LAST number from each line as page number.
        """
        entries = []
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
            
            numbers = re.findall(r'\d+', line)
            if not numbers:
                continue
            
            page_number = int(numbers[-1])
            last_num_pos = line.rfind(numbers[-1])
            if last_num_pos > 0:
                title = line[:last_num_pos].strip()
                title = re.sub(r'[\.\-\s]+$', '', title)
                
                has_arabic = bool(re.search(r'[\u0600-\u06FF]', title))
                is_reasonable_page = 1 <= page_number <= 2000
                is_long_enough = len(title) >= 3
                
                if has_arabic and is_reasonable_page and is_long_enough:
                    entries.append(TocEntry(
                        title=title,
                        page_number=page_number,
                        level="Section"
                    ))
        
        return entries if len(entries) >= 3 else []
