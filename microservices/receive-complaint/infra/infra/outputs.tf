output "lambda_arn" {
  value = aws_lambda_function.receive_complaint.arn
}

output "lambda_name" {
  value = aws_lambda_function.receive_complaint.function_name
}

output "lambda_invoke_arn" {
  value = aws_lambda_function.receive_complaint.invoke_arn
}
