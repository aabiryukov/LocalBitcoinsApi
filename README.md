[![Build Status](https://sitronics.visualstudio.com/GithubPipelineTest/_apis/build/status/aabiryukov.LocalBitcoinsApi?branchName=master)](https://sitronics.visualstudio.com/GithubPipelineTest/_build/latest?definitionId=25?branchName=master)
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
To create an instance of LocalBitcoinsClient:
```
var apiKey = "Your_Key";
var apiSecret = "Your_Secret";
var lbClient = new LocalBitcoinsClient(apiKey, apiSecret);
```
After creating an instance next and more API methods are available:
```
var mySelf = await lbClient.GetMyself();
Console.WriteLine("My name is: " + mySelf.data.username);

var accountInfo = await lbClient.GetAccountInfo("SomeAccountName");
var dashboard = await lbClient.GetDashboard();
var ownAds = await lbClient.GetOwnAds();
var walletBalance = await lbClient.GetWalletBalance();
var contactInfo = await lbClient.GetContactInfo("7652822");
var contactUrl = await lbClient.CreateContact("11534457", 0.1M, "My message");

// Full list of methods you can find in the project sources
```

