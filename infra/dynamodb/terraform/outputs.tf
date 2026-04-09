output "complaints_table_name" {
  value = aws_dynamodb_table.complaints.name
}

output "complaints_table_arn" {
  value = aws_dynamodb_table.complaints.arn
}

output "categories_table_name" {
  value = aws_dynamodb_table.categories.name
}

output "categories_table_arn" {
  value = aws_dynamodb_table.categories.arn
}

output "daily_metrics_table_name" {
  value = aws_dynamodb_table.daily_metrics.name
}

output "daily_metrics_table_arn" {
  value = aws_dynamodb_table.daily_metrics.arn
}
