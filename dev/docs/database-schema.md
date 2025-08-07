# Database Schema

## Overview

Jiro uses Entity Framework Core with a relational database to store conversation data, user information, and session management.

## Core Tables

### Users and Authentication

- **AppUser** - User accounts and profile information
- **AppRole** - User roles and permissions
- **AppUserRole** - Many-to-many relationship between users and roles

### Conversation Management

- **ChatSession** - Individual chat sessions between users and the AI
- **Message** - Individual messages within chat sessions

## Entity Relationships

```cs
AppUser (1) ←→ (Many) ChatSession
ChatSession (1) ←→ (Many) Message
AppUser (Many) ←→ (Many) AppRole (via AppUserRole)
AppUser (1) ←→ (Many) RefreshToken
```

## Message Types

The system supports different message types:

- User messages
- AI assistant responses  
- System messages
- Error messages

## Database Configuration

The database context is configured in `JiroContext.cs` with:

- Entity configurations
- Relationship mappings
- Database constraints
- Indexing for performance

For implementation details, see the Infrastructure namespace in the API documentation.
