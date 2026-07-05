from models.dto import ExtractionResponse


def test_extraction_response_model_can_be_created():
    response = ExtractionResponse(success=True, page_number=3, concepts=[])

    assert response.success is True
    assert response.page_number == 3
    assert response.concepts == []
    assert response.error_message is None
