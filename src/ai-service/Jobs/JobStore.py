import threading
from typing import Dict, Optional

from models.dto import JobRecord, JobStatus, PageResult


class JobStore:
    """
    Thread-safe in-memory store for extraction job state.

    IMPORTANT LIMITATION — read this before you forget it exists:
    This is in-memory only. If this process restarts while a job is
    "Processing", that job's state is gone forever and .NET will get a 404
    on GET /jobs/{job_id} indefinitely (it should treat "job disappeared"
    as equivalent to "Failed" on its side, with a reasonable timeout).

    This is fine for a single instance of this sidecar. The moment you run
    more than one replica behind a load balancer, a poll request can land
    on a different instance than the one doing the work, and this breaks
    silently. At that point, move this to Redis (simple TTL'd keys) or a
    Postgres table — the interface below is deliberately narrow so that
    swap is a small, contained change.
    """

    def __init__(self):
        self._jobs: Dict[str, JobRecord] = {}
        self._lock = threading.Lock()

    def create(self, job_id: str, book_id: str, pages_total: int = 0,
               page_start: int = 1, page_end: Optional[int] = None) -> JobRecord:
        record = JobRecord(job_id=job_id, book_id=book_id, pages_total=pages_total,
                           page_start=page_start, page_end=page_end)
        with self._lock:
            self._jobs[job_id] = record
        return record

    def get(self, job_id: str) -> Optional[JobRecord]:
        with self._lock:
            return self._jobs.get(job_id)

    def add_page_result(self, job_id: str, page_result: PageResult) -> None:
        with self._lock:
            job = self._jobs[job_id]
            job.pages.append(page_result)
            job.pages_done += 1

    def mark_ready(self, job_id: str) -> None:
        with self._lock:
            job = self._jobs[job_id]
            job.status = JobStatus.READY
            job.status_message = "Extraction completed successfully."

    def mark_failed(self, job_id: str, error_message: str) -> None:
        with self._lock:
            job = self._jobs[job_id]
            job.status = JobStatus.FAILED
            job.status_message = error_message