output "lambda_arn" {
  value = aws_lambda_function.classify_complaint.arn
}

output "lambda_name" {
  value = aws_lambda_function.classify_complaint.function_name
}
