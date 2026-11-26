# Researching Instapaper sync capabilities

Label: wayfinder:research
Type: research
Status: resolved

## Question

What does Instapaper's supported integration surface allow for this flow? Verify from authoritative sources whether a client can add bookmarks, later retrieve their completion/read state and any completion timestamp, authenticate, identify existing bookmarks, and what relevant limits or constraints apply.

## Answer

Instapaper API v2 supports adding URLs, listing bookmarks, retrieving a stable bookmark ID and its current `archived` state, and incremental account-wide pulls using `since`. It does **not** document an archive/completion timestamp: `time` is the save time and `progress.timestamp` records reading progress. Use OAuth 2 for new integrations; users authorize through Instapaper, and the app must be approved for users other than its owner. V2 suppresses duplicate URLs but updates and moves the existing bookmark, so retrying an uncertain add has side effects. Bookmark listing is paginated up to 500 records; requests can be rate-limited with HTTP 429, but no numeric request quota is published.

Full findings and source citations: [Instapaper API capabilities](../research/instapaper-api-capabilities.md).
