# Terraform - DynamoDB

## Caminho
`infra/dynamodb/terraform`

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\dynamodb\terraform

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan
terraform apply
```

## Outputs importantes
- `complaints_table_name`
- `complaints_table_arn`
- `categories_table_name`
- `categories_table_arn`

Use esses outputs nos módulos das Lambdas.

