from services.json_extraction import parse_json_lenient, _escape_invalid_backslashes


def test_plain_json_object_parses():
    data, reason = parse_json_lenient('{"success": true, "page_number": 1, "concepts": []}')
    assert reason is None
    assert data["success"] is True
    assert data["concepts"] == []


def test_markdown_fenced_json_parses():
    raw = "```json\n{\"success\": true, \"page_number\": 2, \"concepts\": []}\n```"
    data, reason = parse_json_lenient(raw)
    assert reason is None
    assert data["page_number"] == 2


def test_json_embedded_in_prose_parses():
    raw = 'Here is the result:\n{"success": true, "page_number": 3, "concepts": []}\nHope that helps!'
    data, reason = parse_json_lenient(raw)
    assert reason is None
    assert data["page_number"] == 3


def test_latex_backslashes_are_preserved_not_deleted():
    # The old _fix_json_escapes turned \frac -> frac and \times -> TAB+imes.
    # Repair must PRESERVE the backslash so LaTeX survives.
    raw = '{"success": true, "page_number": 1, "concepts": [' \
          '{"title": "t", "content": "$\\frac{a}{b}$ and $F = m \\times a$", "keywords": ["k1","k2","k3"]}]}'
    data, reason = parse_json_lenient(raw)
    assert reason is None
    content = data["concepts"][0]["content"]
    assert "\\frac" in content
    assert "\\times" in content


def test_escape_helper_doubles_stray_backslash():
    assert _escape_invalid_backslashes(r'"\frac"') == r'"\\frac"'


def test_escape_helper_keeps_quote_and_slash_escapes():
    # \" and \\ and \/ are the only letter-free escapes we preserve; they must
    # survive untouched so structural JSON stays valid.
    assert _escape_invalid_backslashes(r'"a \"q\" b \\ c \/ d"') == r'"a \"q\" b \\ c \/ d"'


def test_escape_helper_doubles_backslash_n_to_protect_nabla():
    # \n is deliberately doubled, not honoured as newline: LaTeX \nabla starts
    # with \n, and mangling equations is worse than losing an escaped newline.
    assert _escape_invalid_backslashes(r'"\nabla"') == r'"\\nabla"'


def test_escape_helper_keeps_unicode_escape():
    assert _escape_invalid_backslashes(r'"ف"') == r'"ف"'


def test_empty_response_reports_reason():
    data, reason = parse_json_lenient("")
    assert data is None
    assert reason == "empty_response"


def test_non_object_json_rejected():
    data, reason = parse_json_lenient("[1, 2, 3]")
    assert data is None
    assert reason == "not_an_object"
