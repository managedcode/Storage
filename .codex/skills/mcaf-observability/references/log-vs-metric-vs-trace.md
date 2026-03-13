# Logs vs Metrics vs Traces

Use the right signal for the job.

## Logs

Best for:

- discrete events
- errors with context
- audit-style diagnostic detail

## Metrics

Best for:

- rates
- latency
- saturation
- alert thresholds and trend tracking

## Traces

Best for:

- following one request or workflow through multiple boundaries
- locating where latency or failure enters a chain

## Practical Rule

If you need history and context, use logs.
If you need aggregation and alerting, use metrics.
If you need end-to-end flow analysis, use traces or correlation IDs.
