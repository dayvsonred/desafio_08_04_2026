output "classification_queue_url" {
  value = aws_sqs_queue.classification.url
}

output "classification_queue_arn" {
  value = aws_sqs_queue.classification.arn
}

output "classification_dlq_arn" {
  value = aws_sqs_queue.classification_dlq.arn
}

output "processing_queue_url" {
  value = aws_sqs_queue.processing.url
}

output "processing_queue_arn" {
  value = aws_sqs_queue.processing.arn
}

output "processing_dlq_arn" {
  value = aws_sqs_queue.processing_dlq.arn
}

output "metrics_queue_url" {
  value = aws_sqs_queue.metrics.url
}

output "metrics_queue_arn" {
  value = aws_sqs_queue.metrics.arn
}

output "metrics_dlq_arn" {
  value = aws_sqs_queue.metrics_dlq.arn
}
