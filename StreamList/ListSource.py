import clr
clr.AddReferenceToFile("StreamList.dll")
from System import *
from Solar import *
from Solar.Models import *
from NekoVampire import StreamList

sl = StreamList("���[�U�[��", "���X�g��")

def GetStatuses(client, range):
    return sl.GetStatuses(client, range)

def StreamEntryMatches(entry):
    return sl.StreamEntryMatches(entry)

Pagable = sl.Pagable
# sl.LocalData = LocalData