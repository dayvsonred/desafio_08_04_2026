output "lambda_name" {
  value = aws_lambda_function.daily_metrics.function_name
}

output "lambda_invoke_arn" {
  value = aws_lambda_function.daily_metrics.invoke_arn
}
