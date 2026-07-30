from models.dto import ExtractionResponse, ExtractedConcept
from services.extraction_validation import validate_extraction


def _concept(title="قانون نيوتن الثاني", content="محتوى تعليمي واضح",
             keywords=("قانون نيوتن", "التسارع", "القوة")):
    return ExtractedConcept(title=title, content=content, keywords=list(keywords))


def test_good_concept_accepted():
    resp = ExtractionResponse(success=True, page_number=1, concepts=[_concept()])
    report = validate_extraction(resp, source_has_content=True)
    assert report.accepted is True
    assert report.score == 1.0
    assert report.flagged_concepts == 0


def test_empty_concepts_with_source_content_rejected():
    resp = ExtractionResponse(success=True, page_number=1, concepts=[])
    report = validate_extraction(resp, source_has_content=True)
    assert report.accepted is False
    assert "empty_concepts_with_source_content" in report.issues


def test_empty_concepts_without_source_content_accepted():
    # Blank / TOC / header page — empty is legitimate, not a failure.
    resp = ExtractionResponse(success=True, page_number=1, concepts=[])
    report = validate_extraction(resp, source_has_content=False)
    assert report.accepted is True


def test_missing_title_is_flagged():
    resp = ExtractionResponse(success=True, page_number=1, concepts=[_concept(title="")])
    report = validate_extraction(resp, source_has_content=True)
    assert "missing_title" in report.issues
    assert report.flagged_concepts == 1


def test_too_few_keywords_flagged():
    resp = ExtractionResponse(success=True, page_number=1, concepts=[_concept(keywords=("واحد",))])
    report = validate_extraction(resp, source_has_content=True)
    assert "too_few_keywords" in report.issues


def test_too_many_keywords_flagged():
    resp = ExtractionResponse(
        success=True, page_number=1,
        concepts=[_concept(keywords=tuple(f"k{i}" for i in range(9)))],
    )
    report = validate_extraction(resp, source_has_content=True)
    assert "too_many_keywords" in report.issues


def test_only_generic_keywords_flagged():
    resp = ExtractionResponse(
        success=True, page_number=1,
        concepts=[_concept(keywords=("فيزياء", "علوم", "مادة"))],
    )
    report = validate_extraction(resp, source_has_content=True)
    assert "only_generic_keywords" in report.issues


def test_duplicate_concepts_flagged():
    resp = ExtractionResponse(
        success=True, page_number=1,
        concepts=[_concept(), _concept()],
    )
    report = validate_extraction(resp, source_has_content=True)
    assert any(i.startswith("duplicates:") for i in report.issues)


def test_all_empty_concepts_rejected():
    resp = ExtractionResponse(
        success=True, page_number=1,
        concepts=[ExtractedConcept(title="", content="", keywords=[])],
    )
    report = validate_extraction(resp, source_has_content=True)
    assert report.accepted is False
    assert "all_concepts_empty" in report.issues


def test_unsuccessful_response_rejected():
    resp = ExtractionResponse(success=False, page_number=1, concepts=[], error_message="boom")
    report = validate_extraction(resp, source_has_content=True)
    assert report.accepted is False
    assert "not_success" in report.issues
