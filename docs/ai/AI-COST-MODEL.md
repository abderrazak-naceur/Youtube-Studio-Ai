# AI Cost Model

## Cost dimensions

### LLM

- input tokens
- output tokens
- cached tokens
- reasoning/compute where billed

### TTS

- characters or seconds
- voice/model

### Image

- images generated
- resolution
- model

### Video

- generated seconds
- resolution
- model
- retries

### Dedicated inference

- GPU/CPU hours
- replicas
- idle time

Hugging Face Inference Providers currently uses pay-as-you-go provider pricing, while dedicated Inference Endpoints charge by running compute time. citeturn0search0turn0search3

## Margin rules

The router must know the maximum allowed cost for each customer plan and job type.

Example:

`MaxGenerationCost = ExpectedCustomerValue × AI_Budget_Percentage`

A job exceeding the budget should be downgraded, queued for approval or rejected.

## Required ledger

`provider`, `model`, `job_id`, `input_units`, `output_units`, `unit_price`, `total_cost`, `currency`, `customer_id`, `timestamp`.
