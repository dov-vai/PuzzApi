# Puzz API

API for the Puzz project which is available [here](https://github.com/dov-vai/Puzz)

- EF Core for managing the database.
- BCrypt for password salting and hashing.
- JWT Tokens for authorization. 

# Building

Generate a private RSA key file using OpenSSL for validating JWT Tokens:

```bash
openssl genrsa -out key.pem 2048
```
The file should be generated in the root directory of the project,
otherwise change the location of the generated key, see [Configuring](#configuring)

Assuming .NET is installed, it can be run immediately with:
```
dotnet run
```

Or built using:
```
dotnet build -c Release
```

# Configuring

The private key file location can be changed with the `JWT:PrivateKeyFile` parameter.
Replace `key.pem` with the location of your own.

In the same way, the Sqlite database location can be changed too with the `ConnectionString:UserContext` parameter.

# Contributing
Pull requests are always welcome.

# LICENSE

GNU General Public License 3.0 or later.

See [LICENSE](LICENSE) for the full text.