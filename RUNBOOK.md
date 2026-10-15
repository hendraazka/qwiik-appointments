# Runbook: Appointment Booking API

## 1. Health and quick checks
| Check | Command | Healthy |
|---|---|---|
| Service up | `curl -s localhost:5080/health` | `{"status":"Healthy"}` (503 means the database is unreachable) |
| API docs | open `/swagger` | page loads |
| Today's queue | `curl -s localhost:5080/api/queue` | JSON array |

## 2. Reading logs
Every request is assigned a correlation ID. It is returned in the `X-Correlation-ID`
response header, appears in every log line as `[<id>]`, and is included as
`correlationId` in error responses (RFC 7807 problem details).

1. Ask the reporter for the correlation ID (from the error body or header).
2. Search the log output for that ID to see the whole request, including exceptions.
3. Logs are written to the console (stdout): `docker logs <container>` in Docker.

## 3. Severity classification
| Level | Definition | Example | Target response |
|---|---|---|---|
| P1 Critical | Service down or no booking possible for anyone | `/health` returns 503, all bookings return 500 | Immediate |
| P2 High | Degraded for many users or incorrect data | Sustained 409/500 on booking, wrong token numbers, bookings landing on wrong dates | Within 1 hour |
| P3 Medium | One feature failing, workaround exists | Setting max per day or adding off days fails | Same business day |
| P4 Low | Cosmetic or documentation | Swagger text, typo in a message | Next sprint |

## 4. Common symptoms
| Symptom | Likely cause | Action |
|---|---|---|
| 400 on booking | Business rule (past date, blank name) or validation | Read `detail` in the response; not a server fault |
| 409 on booking | Concurrent booking conflict persisted after 5 retries | See section 5; check request volume on the same day |
| 500 | Unhandled exception | Find the correlation ID in the logs and read the stack trace |
| Booking moved to another day | Requested day is full or an off day | Expected; check `movedFromRequestedDate` and the off-day list |
| "Cannot book in the past" right after midnight | Server time zone mismatch | The app evaluates "today" in Asia/Jakarta; confirm the container has tzdata |

## 5. Example RCA: conflicting tokens under concurrent booking
Found during testing, not in production.

- **Impact:** Five simultaneous bookings for the same day returned 201, 201, 409, 409, 409.
  Valid customers were rejected while the day had capacity for 20.
- **Data integrity:** Not affected. The queue contained tokens 1 and 2 with no duplicates.
- **Root cause:** Token assignment is read-then-write (last token + 1). Parallel requests read
  the same value and tried to insert the same token. The unique index on `(Date, TokenNumber)`
  rejected the losers (SQLite constraint error), which the API mapped to 409.
- **Detection:** Parallel curl test; the 409 responses carried correlation IDs that were
  matched to the log lines showing the constraint failure.
- **Fix:** The repository translates the unique violation into `SlotConflictException` and
  clears the EF change tracker; `BookingService` retries up to 5 times with a randomized
  10 to 60 ms backoff, recomputing the token each time.
- **Verification:** Re-ran five parallel bookings for a new date.
  Result: <paste the five HTTP codes and the final queue here>
- **Prevention and remaining risk:** Retries cover small bursts only. Higher concurrency needs a
  database with proper row-level concurrency control (e.g. Azure SQL) and a serializable
  transaction or a per-day counter row. Alert on a sustained rise in 409 responses.

## 6. Known limitations
- SQLite: single writer, so heavy write load can cause "database is locked" errors.
- No authentication, no cancellation endpoint, no rate limiting.
