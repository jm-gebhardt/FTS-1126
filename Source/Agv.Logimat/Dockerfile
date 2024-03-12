FROM toke/mosquitto

WORKDIR /app

COPY . /app

ENV MSG_FILE=InstaPick-1.txt
ENV TOPIC=agv/status
ENV RABBITMQ_HOST=localhost
ENV RABBITMQ_USER=guest
ENV RABBITMQ_PASSWORD=guest

CMD bash ./publisher.sh $MSG_FILE
