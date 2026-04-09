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
