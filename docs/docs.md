# Docs

## Prerequisites

- .NET Core 3.1
- SQL Server 2019

## Notes

- You can control the work folder location through the **TMP** environment variable.
- Disable the anti-malware scan on the work folder path because the anti-malware process load is going to high during the update packages extraction that decreases the data collection performance. The update packages extraction does create & delete files many times.
