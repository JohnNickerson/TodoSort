# Resuming after a partial sync

Label: wayfinder:grilling
Type: grilling
Blocked by: 02, 05

## Question

After service or network failure partway through a push or pull, what state must TodoSort checkpoint and what should a later run retry or reconcile? Specify safe recovery for uncertain bookmark-add outcomes (Instapaper updates an existing URL instead of creating a duplicate) and paginated incremental pulls (`since` plus `deleted_ids`), including what the user sees when a run is incomplete.
