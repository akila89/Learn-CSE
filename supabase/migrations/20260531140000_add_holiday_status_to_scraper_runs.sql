ALTER TABLE scraper_runs
  DROP CONSTRAINT scraper_runs_status_check;

ALTER TABLE scraper_runs
  ADD CONSTRAINT scraper_runs_status_check
    CHECK (status IN ('success', 'partial', 'failed', 'holiday'));
