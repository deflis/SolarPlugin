import clr
clr.AddReferenceToFile("StreamList.dll")
from System import *
from Solar import *
from Solar.Models import *
from NekoVampire import StreamList

# ユーザー名とリスト名に自分の使いたいリストを入れてください。（@ユーザー名/リスト名）
sl = StreamList("ユーザー名", "リスト名")

def GetStatuses(client, range):
    try:
        return sl.GetStatuses(client, range)
    except RateLimitExceededException, e:
        ProgressBlock(e.Message, True)
        return None

def StreamEntryMatches(entry):
    return sl.StreamEntryMatches(entry)

Pagable = sl.Pagable
# sl.LocalData = LocalData