# Terraform - SQS

## Caminho
`infra/sqs/terraform`

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\sqs\terraform

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan
terraform apply
```

## Outputs importantes
- `classification_queue_url`
- `classification_queue_arn`
- `processing_queue_url`
- `processing_queue_arn`
- `metrics_queue_url`
- `metrics_queue_arn`
- `classification_dlq_arn`
- `processing_dlq_arn`
- `metrics_dlq_arn`

Use esses outputs nos módulos das Lambdas.
