# Email Service

This is a ASP.NET Core Service that handles basic email authentication and sending custom email messages via Azure Communications Service.

- ### [Technologies Used](#technologies-used)
- ### [Endpoints](#endpoints)

## Technologies Used

### Data Interaction
- Entity Framework Core
- Azure SQL Database & Server

### Email Interaction
- Azure Communication Service
- SmtpClient

### Testing
- NSubstitute
- XUnit

### Logging
- OpenTelemetry (reporting to Azure)

### Others
- Dependency Injection
- SwaggerUI & OpenAPI 

## Endpoints

### `POST /api/authenticate`

#### Fields
`Email: string, required`: Email to send the authentication code

#### Description
Sends a GUID as an authentication code to the specified email. The code is stored on server-side, and is used in authenticating users, creating session cookies, etc.

One can only send one authentication requests per day to the same email. 

This endpoint is rate-limited.

### `POST /api/session`

#### Fields
`Email: string, required`: Email to create authentication cookie with\
`AuthenticationCode: GUID, required`: Authentication code received by the user

#### Description
Creates a session cookie based on the authentication code & email received, and logs user in.

Will only create one if the authentication code matches the one in the database.

### `DELETE /api/session`

#### Fields
None

#### Description
Removes the session cookie and logs user out.

### `POST /api/send`

#### Fields
`Recipient: required string`: Recipient of the email message.\
`Subject: string` : Subject of the email\
`Body: string`: Body of the email

#### Description

Crafts an email message and queues the message via `SmtpClient`.

For now, will only accept email messages that are towards oneself (will check your cookie). User must be authenticated.

### `POST /api/sendtoshin`

#### Fields
`Recipient: string, deprecated`: Ignored in this endpoint\
`Subject: string` : Subject of the email\
`Body: string`: Body of the email

#### Description
Crafts an email message pointed towards Shin, and queues the message.

Allows CORS requests to accept requests from frontend.