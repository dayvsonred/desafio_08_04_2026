output "lambda_arn" {
  value = aws_lambda_function.process_classified_complaint.arn
}

output "lambda_name" {
  value = aws_lambda_function.process_classified_complaint.function_name
}
