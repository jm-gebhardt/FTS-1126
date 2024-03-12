#!/usr/bin/env bash

# File with JSON messages
file=$1

echo "Sending messages from file" $file
echo "to topic" $TOPIC

# Forever
while true
do
# Read each line in the file
while IFS= read -r msg
do
  echo "Publishing $msg"
  # Publish the message
  echo "$msg" | mosquitto_pub -h $RABBITMQ_HOST -p 1883 -u $RABBITMQ_USER -P $RABBITMQ_PASSWORD -t $TOPIC -s

  # Wait for a while before sending the next message
  sleep 1
done < "$file"
done
