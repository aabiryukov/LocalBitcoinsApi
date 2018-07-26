[![Nuget.org](https://img.shields.io/nuget/v/LocalBitcoinsApi.svg?style=flat)](https://www.nuget.org/packages/LocalBitcoinsApi)

# LocalBitcoinsApi
It is LocalBitcoins.com API .NET Library

Package in Nuget: https://www.nuget.org/packages/LocalBitcoinsApi/

This LocalBitcoins.com API wrapper written in C# provides a quick access to most available LocalBitcoins features.
Original API documentation available on the official web site: https://localbitcoins.com/api-docs/

You can look for code examples in LocalBitcoins.Test project which is a part of the solution.
# Installation
To install LocalBitcoinsApi with Nuget, run the following command in the Package Manager Console
```
PM> Install-Package LocalBitcoinsApi
```
# Code examples
To create an instance of LocalBitcoinsApi:
```
var apiKey = "Your_Key";
var apiSecret = "Your_Secret";
var lbClient = new LocalBitcoinsAPI(apiKey, apiSecret);
```
After creating an instance next and more API methods are available:
```
var mySelf = lbClient.GetMyself();
var accountInfo = lbClient.GetAccountInfo("SomeAccountName");
var dashboard = lbClient.GetDashboard();
var ownAds = lbClient.GetOwnAds();
var walletBalance = lbClient.GetWalletBalance();
var contactInfo = lbClient.GetContactInfo("7652822");
var contactUrl = lbClient.CreateContact("11534457", 0.1M, "My message")

// Full list of methods you can find in the project sources
```

