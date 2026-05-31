-- idx_companies_symbol_root is redundant: symbol_root NOT NULL UNIQUE already
-- creates an implicit B-tree index that covers every lookup on the column.
DROP INDEX IF EXISTS idx_companies_symbol_root;
