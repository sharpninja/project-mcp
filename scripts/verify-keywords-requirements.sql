-- Verify keywords and requirements in PostgreSQL.
-- Run with: psql "YOUR_CONNECTION_STRING" -f scripts/verify-keywords-requirements.sql
-- Or from psql: \i scripts/verify-keywords-requirements.sql

\echo '=== Keywords ==='
SELECT id, enterprise_id, display_id, name FROM keywords ORDER BY enterprise_id, display_id;

\echo ''
\echo '=== Requirements (with keyword name) ==='
SELECT r.id, r.project_id, r.display_id, LEFT(r.title, 50) AS title, r.keyword_id, k.name AS keyword_name
FROM requirements r
LEFT JOIN keywords k ON k.id = r.keyword_id
ORDER BY r.project_id, r.display_id;

\echo ''
\echo '=== Counts ==='
SELECT (SELECT COUNT(*) FROM keywords) AS keywords_count, (SELECT COUNT(*) FROM requirements) AS requirements_count;
