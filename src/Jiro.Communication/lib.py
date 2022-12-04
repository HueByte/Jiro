import os


class colors:
    JIRO = '\x1b[38;2;0;250;154m'
    USER = '\x1b[38;2;255;166;158m'
    ENDC = '\033[0m'


def get_username():
    return os.getlogin()
