"""
Deterministic quality validation for extraction results.

Shared by every provider and by ExtractionManager so that "did this page
extract well enough to index?" has ONE answer, not one per provider.

Design rules (from the extraction-quality requirements):
- Deterministic checks only — no AI-based scoring, no fuzzy heuristics that
  could reject good output.
- Never fabricate or mutate educational content here; validation observes,
  it does not rewrite.
- A failing result is a *fallback trigger*, not a silent accept and not a
  hard drop. The manager decides whether to keep a flagged best-effort result.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import List

from models.dto import ExtractionResponse


# Generic subject-level terms that are useless as search keywords on their own.
# A concept whose keywords are ONLY drawn from this set is low quality.
GENERIC_KEYWORDS = {
    "فيزياء", "رياضيات", "علوم", "كيمياء", "أحياء", "جغرافيا", "تاريخ",
    "لغة", "قواعد", "درس", "وحدة", "فصل", "مادة", "تعليم", "منهج",
    "physics", "math", "mathematics", "science", "chemistry", "biology",
    "history", "geography", "lesson", "unit", "chapter", "subject",
}


@dataclass
class QualityReport:
    """Outcome of validating one ExtractionResponse."""
    accepted: bool
    score: float                       # 0.0 - 1.0, deterministic
    issues: List[str] = field(default_factory=list)
    flagged_concepts: int = 0
    total_concepts: int = 0

    def summary(self) -> str:
        """Compact, log-safe one-liner. Never contains textbook content."""
        return (
            f"accepted={self.accepted} score={self.score:.2f} "
            f"concepts={self.total_concepts} flagged={self.flagged_concepts} "
            f"issues={','.join(self.issues) if self.issues else 'none'}"
        )


def _normalize(text: str) -> str:
    return " ".join((text or "").split()).strip().lower()


def validate_extraction(
    response: ExtractionResponse,
    *,
    source_has_content: bool,
    keywords_min: int = 3,
    keywords_max: int = 7,
) -> QualityReport:
    """
    Validate a parsed ExtractionResponse with deterministic checks.

    source_has_content: whether the page's source (text/image) plausibly held
        real educational content. Used ONLY to distinguish "correctly empty"
        (blank/TOC/header page) from "empty despite real content" — the latter
        is a quality failure worth a fallback.

    Returns a QualityReport. Individual per-concept problems lower the score and
    flag the concept but do not by themselves reject the whole page; structural
    problems (empty-when-content-exists, all concepts malformed) do.
    """
    issues: List[str] = []

    if not getattr(response, "success", False):
        return QualityReport(accepted=False, score=0.0, issues=["not_success"])

    concepts = response.concepts or []
    total = len(concepts)

    if total == 0:
        # Empty is legitimate for blank/TOC/header pages, but suspicious when the
        # source clearly had content — that's the "valid JSON, empty concepts"
        # failure mode we must catch.
        if source_has_content:
            return QualityReport(
                accepted=False, score=0.0,
                issues=["empty_concepts_with_source_content"],
                total_concepts=0,
            )
        return QualityReport(accepted=True, score=1.0, total_concepts=0)

    flagged = 0
    seen_signatures: set[str] = set()
    duplicate_count = 0

    for concept in concepts:
        concept_issues: List[str] = []

        title = _normalize(getattr(concept, "title", ""))
        content = _normalize(getattr(concept, "content", ""))
        keywords = [k for k in (getattr(concept, "keywords", []) or []) if _normalize(k)]

        if not title:
            concept_issues.append("missing_title")
        if not content:
            concept_issues.append("empty_content")

        kw_count = len(keywords)
        if kw_count < keywords_min:
            concept_issues.append("too_few_keywords")
        elif kw_count > keywords_max:
            concept_issues.append("too_many_keywords")

        if keywords:
            non_generic = [k for k in keywords if _normalize(k) not in GENERIC_KEYWORDS]
            if not non_generic:
                concept_issues.append("only_generic_keywords")

        # Duplicate detection: same title+content signature.
        signature = f"{title}||{content}"
        if signature in seen_signatures:
            duplicate_count += 1
            concept_issues.append("duplicate_concept")
        else:
            seen_signatures.add(signature)

        if concept_issues:
            flagged += 1
            for issue in concept_issues:
                if issue not in issues:
                    issues.append(issue)

    # A concept with neither title nor content carries no indexable signal.
    empty_concepts = sum(
        1 for c in concepts
        if not _normalize(getattr(c, "title", "")) and not _normalize(getattr(c, "content", ""))
    )

    # Score: fraction of concepts that are clean.
    clean = total - flagged
    score = clean / total if total else 0.0

    # Rejection rules (fallback triggers):
    #  - every concept is flagged AND none has usable content -> garbage
    #  - all concepts empty -> garbage
    accepted = True
    if empty_concepts == total:
        accepted = False
        if "all_concepts_empty" not in issues:
            issues.append("all_concepts_empty")
    elif flagged == total and clean == 0 and score == 0.0:
        accepted = False

    if duplicate_count:
        issues.append(f"duplicates:{duplicate_count}")

    return QualityReport(
        accepted=accepted,
        score=round(score, 2),
        issues=issues,
        flagged_concepts=flagged,
        total_concepts=total,
    )
