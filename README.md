s3uploader
==========

Upload a folder to s3 with a lower key name.

configuration
=============

open the App.config, and fill out with s3 bucket name and key/secret.

```xml
<configuration>
  <appSettings>
    <add key="BucketName" value="mybucketname" />
    <add key="AWSAccessKey" value="your access key" />
    <add key="AWSSecretKey" value="your secret key" />
  </appSettings>
</configuration>
```

Usage:
======

```text
c:\>s3upload.exe project   #this will upload all files to 'mybucketname/project/xxxx' with lower key name.
```
