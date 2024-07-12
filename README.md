# UPXpatcher
*A UPX patcher to prevent the use of "-d"*

*Prevents signature matching of UPX by Detect it Easy*

Currently modifies the following:

Modifys the section characteristics

Modifys section headers

Modifys the PE header fields such as timestamp and checksum

Modifys the import table by adding dummy functions or modifying existing ones

Modifys the UPX header to confuse signature-based detections

Adds Random padding length between 16 and 64 bytes
