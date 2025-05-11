# Semantic Kernel Chat Application

A .NET 9 console application that implements a chat interface using Microsoft's Semantic Kernel and Azure OpenAI services.

## Features
- Console-based chat interface with Azure OpenAI
- Real-time response streaming
- Conversation history management

## Setup
1. Configure Azure OpenAI and GitHub credentials in `appsettings.Development.json`:
```json
{
  "ModelName": "",
  "Endpoint": "",
  "ApiKey": "",
  "GIT_PAT": "",
  "GIT_NAME": ""
}
```
## Prompts

```
SetRepo("...")
```
```
Create release notes from the last 10 commits, bump patch, commit the updated version.json and push to origin/master.
```