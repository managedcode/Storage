# Observability

Observability answers one practical question: how will the team detect, diagnose, and explain failure in this flow.

## Minimum Signals

- logs for discrete events and errors
- metrics for rates, latency, saturation, and availability
- traces or correlated identifiers across boundaries
- alerts for conditions that need action

## Design Rules

- instrument important user or system flows, not everything equally
- make failures diagnosable without remote debugging
- include enough context to correlate work across services or jobs
- define alerts on user impact, not only infrastructure noise

## Done Criteria

- success and failure paths are visible
- operators know what to look at first
- the team can distinguish symptoms from root-cause clues
