import os


class colors:
    JIRO = '\x1b[38;2;0;250;154m'
    USER = '\x1b[38;2;255;166;158m'
    ENDC = '\033[0m'


def get_username():
    return os.getlogin()


logo = """
@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@&BGGPPPPPPPPPGGB#&@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@&BGPPPPPPPPPPPPPPPPPPPPG#@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@BPPPPPPPPPPPPPPPPPPPPPPPPPPPB@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@#PPPPGP&&GPPPPPPPPPPPPPPPPPPPPPP&@@@@@@@@@@@@@@
@@@@@@@@@@@@BPPPPB@@@@&PPPPPPPPPPPPPPPPPPPPPPP&@@@@@@@@@@@@@
@@@@@@@@@@@BPPPPP##7^~@PPPPPPPPPBBBBGPPPPPPPPPP&@@@@@@@@@@@@
@@@@@@@@@@#PPPPPP##!7:G#PPPPPPPPBBBBGPPPPPPPPPPP&@@@@@@@@@@@
@@@@@@@@@@GPPPPPP&7   .&GPPPPPPPPP#BPPPPPPPPPPPPG&@@@@@@@@@@
@@@@@@@@@@&PPPPPP@~    :&G#&BGPPPP&@&G#&BP&&BBBP&@@@@@@@@@@@
@@@@@@@@@@@PB#BP&@&5!77~?B&@5G#&&&@&@@&@&&B7..J&@@@@@@@@@@@@
@@@@@@@@@@@@5^&&BJ?@@###&~~#B .!@#PPPG&@Y.     B@@@@@@@@@@@@
@@@@@@@@@@@@: #P  J&Y555P&  .  .@55555P&       &@@@@@@@@@@@@
@@@@@@@@@@@@B .B. ~&GPPPP@:     P#GGGB#?    .!#@@@@@@@@@@@@@
@@@@@@@@@@@@@@PG&^ .75PP5^       :!!!:   7&@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@B!        !~       .~G@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@&P?!^^:::^^~7YG&@@@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#::&@@@@@@@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@@@@@@@BP#^?5?G&@@@@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@@@@#&J 7&&!   .#P&@@@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@@&! ~B?&BB#?..GJ .7&@@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@&. ..BG!B#^7GG? .^..J@@@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@@?Y^... ?G#^ ::. ^&Y??7&@@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@@7~5G .. Y5#^....:#^^~. .#@@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@@G  .#^?5 5Y#:....B5YJ7JJ!:&@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@&....Y5 Y5GY#:...~&@P??!~: !@@@@@@@@@@@@@@
@@@@@@@@@@@@@@@@@@!.YPBBB  7@G#....BB^..:~?7..#@@@@@@@@@@@@@
"""