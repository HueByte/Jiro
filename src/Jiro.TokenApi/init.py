import json
import os


class Config(object):
    def __init__(self, host='0.0.0.0', port: int = 8000):
        self.host = host
        self.port = port


configPath = 'config.json'

if os.path.isfile(configPath):
    with open(configPath, 'r') as f:
        config_data = json.load(f)
        config = Config(**config_data)
else:
    config = Config()
    with open(configPath, 'w') as f:
        json.dump(config.__dict__, f)
