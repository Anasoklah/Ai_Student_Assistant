class PromptBuilder:
    @staticmethod
    def build_extraction_prompt(text: str) -> str:
        # توجيهات صارمة باللغة الإنجليزية لضمان أعلى دقة والتزام بالهيكل
        return f"""
        You are an expert educational content extractor specialized in the Syrian school curriculum.
        Your task is to analyze the provided text from a textbook page and extract educational concepts, 
        definitions, explanations, and important questions.

        Source Text to Analyze:
        ---
        {text}
        ---

        Strict Instructions:
        1. Extract the title, content, and keywords in Modern Standard Arabic (الفصحى), preserving the educational meaning from the context.
        2. Segregate the content into logical, separate concept objects based on subheadings or core ideas.
        3. Identify and extract the most relevant keywords for each concept to assist in our vector indexing pipeline.
        4. Ensure the output strictly adheres to the requested JSON schema.
        """