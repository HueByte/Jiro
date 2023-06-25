FROM python:3.9
WORKDIR /app
EXPOSE 80

COPY ./requirements.txt /app/requirements.txt
RUN pip install --no-cache-dir --upgrade -r /app/requirements.txt
COPY ./ /app
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80"]