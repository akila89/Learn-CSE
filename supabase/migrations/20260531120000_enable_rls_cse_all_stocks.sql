-- Enable RLS on cse_all_stocks — authenticated access only (ADR 0013)
-- Stock Manager is post-login only; queries always carry an authenticated JWT.
-- Scraper uses service role key which bypasses RLS.

ALTER TABLE cse_all_stocks ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON cse_all_stocks
  FOR ALL TO authenticated USING (true) WITH CHECK (true);
