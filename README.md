# Examples in C# for the Visit-X API

This repository contains examples and sample use cases on how to use the API provided by Visit-X. The documentation of this API can be found at https://partner.visit-x.net/

In order to access the documentation and run the examples, you will need username and password and an accessKey. Please contact [Christof Mendner](mailto:cm@campoint.net) in order to create an account and get those credentials.

**NOTE:** The code itself should be seen as examples and not be used as is for productive environment, as it lacks error handling, among other things.

The first step should be to enter you credentials in the _ApiCredentials.cs_ class. This will then be shared with all examples in this repository.

## Examples description

The following examples should cover the most common use cases and show how to comminucate with the API in order to achieve them.

### FetchSenders

This example shows how to receive all senders on the Visit-X plattform and get additional information related to them like a list of their galleries.

### QuerySenders

This example shows how to use the senders API with different queries, like querying for male senders or senders younger than a given age.

### TranslateProfile

This examples shows how to handle translations in the sender's profile

### BuyContent

Sender can have chargeable content in their accounts. This example shows how this content can be bought.

### SendPrivateMessages

The Visit-X plattform supports communication between viewers and senders via private messaging. This example shows how to send a message to a given sender.

### GetOnlineStates

This example shows how to setup a websocket in order to get information about the on-/offline state of senders.

### GetAChatWindow
This example shows how to setup a chat session with the first sender available for a chat.
