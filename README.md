# UPXpatcher
*A UPX patcher to prevent the use of "-d"*
*Prevents signature matching of UPX by Detect it Easy*

# Features:
- Modifies the section characteristics
- Modifies section headers
- Modifies the PE header fields such as timestamp and checksum
- Modifies the import table by adding dummy functions or modifying existing ones
- Modifies the UPX header to confuse signature-based detections
- Adds random padding between 16 and 64 bytes
