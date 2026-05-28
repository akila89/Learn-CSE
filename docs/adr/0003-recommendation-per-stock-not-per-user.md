# Recommendation per Stock, not per user

One **Recommendation** is generated per Stock per night, shared across all users. Although Annotations are per-user, the Recommendation prompt aggregates all users' Annotations into a single combined summary before sending to Gemini — one API call, one result stored.

A per-user Recommendation model was rejected because it would multiply Gemini API calls linearly with the number of users, which conflicts with the free-tier constraint. The combined-annotation approach preserves personalisation value (all user context reaches Gemini) while keeping computation proportional to distinct Active Stocks, not user count.

## Consequences

If two users hold contradictory annotations on the same field, both are sent to Gemini as-is. Gemini is expected to reason across conflicting context rather than the app resolving the conflict. This may need revisiting if the user base grows significantly.
