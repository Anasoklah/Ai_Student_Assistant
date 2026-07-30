DOCUMENT PROCESSING FLOW

Read this feature in the following order:

1. Api/Controllers/DocumentIngestionController.cs
   Receives the HTTP upload and maps the API request to UploadDocumentCommand.

2. DocumentUploadAndQueryUseCase.cs
   Validates metadata, creates the Document, stores the temporary file, and queues work.

3. Infrastructure/Documents/BackgroundJobs/DocumentProcessingWorker.cs
   Reads queued requests in the background.

4. Infrastructure/Documents/BackgroundJobs/DocumentBackgroundProcessor.cs
   Opens the temporary file, selects PDF or image extraction, validates extracted content,
   updates status, and requests ingestion.

5. Infrastructure/Documents/Extraction/AiDocumentContentExtractor.cs
   Calls the AI HTTP service and maps provider response objects into Application DTOs.

6. DocumentContentIngestionService.cs
   Splits content into chunks, generates embeddings, and adds chunks to the Document.

7. Infrastructure/Persistence/Repositories/DocumentRepository.cs
   Performs the EF Core and PostgreSQL operations.

LAYER RULE

Api converts HTTP requests and registers services.
Application owns use cases and interfaces. It does not reference Infrastructure or ASP.NET types.
Infrastructure implements Application interfaces and contains HTTP, EF Core, file, and worker code.
Domain contains entities and business concepts only.

QUEUE LIMITATION

InMemoryDocumentProcessingJobQueue loses jobs when the process restarts. Startup reconciliation
marks interrupted documents as failed. Use a durable queue only when jobs must survive restarts.
