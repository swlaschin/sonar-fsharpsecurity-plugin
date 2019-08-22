module SonarAnalyzer.FSharp.UnitTest.TestCases.S2077_ExecutingSqlQueries

open System

type User = {
    Id : string
    Name : string
    }

//---------------------------
// Dummy classes for NetCore
//---------------------------

type Database() =
    member this.ExecuteSqlCommand(query:string) = ()
    member this.ExecuteSqlCommand(query:string, _params:obj[]) = ()
    member this.ExecuteSqlCommandAsync(query:string) = ()
    member this.ExecuteSqlCommandAsync(query:string, _params:obj[]) = ()

type RelationalDatabaseFacadeExtensions =
    static member ExecuteSqlCommand(db:Database,query:string) = ()
    static member ExecuteSqlCommand(db:Database,query:string, _params:obj[]) = ()
    static member ExecuteSqlCommandAsync(db:Database,query:string) = ()
    static member ExecuteSqlCommandAsync(db:Database,query:string, _params:obj[]) = ()

type Query<'T>() =
    member this.FromSql(query:string) = ()
    member this.FromSql(query:string, _params:obj[]) = ()
    member this.ExecuteSqlCommandAsync(query:string) = ()
    member this.ExecuteSqlCommandAsync(query:string, _params:obj[]) = ()

type RelationalQueryableExtensions =
    static member FromSql(q:Query<_>,query:string) = ()
    static member FromSql(q:Query<_>,query:string, _params:obj[]) = ()

type DbContext() =
    let db = Database()
    member this.Database : Database = db
    member this.Query<'T>() : Query<'T> = Query<'T>()

//---------------------------
// Dummy classes for Net46
//---------------------------

type RawSqlString(s:string) = class end
type DbConnection() = class end
type DbTransaction() = class end
type SqlCommandColumnEncryptionSetting = Enabled

type SqlCommand(query:string,connection:DbConnection, transaction:DbTransaction,setting:SqlCommandColumnEncryptionSetting) =
    new(query,connection,transaction) = SqlCommand(query,connection, transaction,Enabled)
    new(query,connection) = SqlCommand(query,connection, DbTransaction())
    new(query) = SqlCommand(query,DbConnection())
    new() = SqlCommand("")
    member val CommandText = "" with get,set

type SqlDataAdapter(query:string,connection:DbConnection,command:SqlCommand) =
    new (query, connection) = SqlDataAdapter(query, connection,SqlCommand())
    new (command) = SqlDataAdapter("", DbConnection(),command)
    new (query, connectionString:string) = SqlDataAdapter(query, DbConnection())
    new () = SqlDataAdapter("", DbConnection())

type OdbcCommand(query:string,connection:DbConnection, transaction:DbTransaction,setting:SqlCommandColumnEncryptionSetting) =
    new(query,connection,transaction) = OdbcCommand(query,connection, transaction,Enabled)
    new(query,connection) = OdbcCommand(query,connection, DbTransaction())
    new(query) = OdbcCommand(query,DbConnection())
    new() = OdbcCommand("")
    member val CommandText = "" with get,set

type OdbcDataAdapter(query:string,connection:DbConnection,command:OdbcCommand) =
    new (query, connection) = OdbcDataAdapter(query, connection,OdbcCommand())
    new (command) = OdbcDataAdapter("", DbConnection(),command)
    new (query, connectionString:string) = OdbcDataAdapter(query, DbConnection())
    new () = OdbcDataAdapter("", DbConnection())

type SqlCeCommand(query:string,connection:DbConnection, transaction:DbTransaction,setting:SqlCommandColumnEncryptionSetting) =
    new(query,connection,transaction) = SqlCeCommand(query,connection, transaction,Enabled)
    new(query,connection) = SqlCeCommand(query,connection, DbTransaction())
    new(query) = SqlCeCommand(query,DbConnection())
    new() = SqlCeCommand("")
    member val CommandText = "" with get,set

type SqlCeDataAdapter(query:string,connection:DbConnection,command:SqlCeCommand) =
    new (query, connection) = SqlCeDataAdapter(query, connection,SqlCeCommand())
    new (command) = SqlCeDataAdapter("", DbConnection(),command)
    new (query, connectionString:string) = SqlCeDataAdapter(query, DbConnection())
    new () = SqlCeDataAdapter("", DbConnection())

type OracleCommand(query:string,connection:DbConnection, transaction:DbTransaction,setting:SqlCommandColumnEncryptionSetting) =
    new(query,connection,transaction) = OracleCommand(query,connection, transaction,Enabled)
    new(query,connection) = OracleCommand(query,connection, DbTransaction())
    new(query) = OracleCommand(query,DbConnection())
    new() = OracleCommand("")
    member val CommandText = "" with get,set

type OracleDataAdapter(query:string,connection:DbConnection,command:OracleCommand) =
    new (query, connection) = OracleDataAdapter(query, connection,OracleCommand())
    new (command) = OracleDataAdapter("", DbConnection(),command)
    new (query, connectionString:string) = OracleDataAdapter(query, DbConnection())
    new () = OracleDataAdapter("", DbConnection())


// ======================================================
// Code used in .NET Core
// ======================================================

type NetCoreProgram() =
    let ConstQuery = ""

    member this.Foo(context:DbContext, query:string, x:int, guid:Guid, [<ParamArray>] parameters:obj[]) =
        context.Database.ExecuteSqlCommand(sprintf "") // Compliant
        context.Database.ExecuteSqlCommand("") // Compliant, constants are safe
        context.Database.ExecuteSqlCommand(ConstQuery) // Compliant, constants are safe
        context.Database.ExecuteSqlCommand("" + "") // Compliant, constants are safe
        context.Database.ExecuteSqlCommand(query) // Compliant, not concat or format
        context.Database.ExecuteSqlCommand("" + query) // Noncompliant
        context.Database.ExecuteSqlCommand(sprintf "%A" parameters) // Noncompliant FP, interpolated string
        context.Database.ExecuteSqlCommand(query, parameters) // Compliant, not concat or format
        context.Database.ExecuteSqlCommand("" + query, parameters) // Noncompliant

        context.Database.ExecuteSqlCommand(sprintf "SELECT * FROM mytable WHERE mycol=%O AND mycol2={0}" parameters.[0]) // Noncompliant, string interpolation
        context.Database.ExecuteSqlCommand(sprintf "SELECT * FROM mytable WHERE mycol=%O%O" x guid) // Noncompliant

        RelationalDatabaseFacadeExtensions.ExecuteSqlCommand(context.Database, query) // Compliant
        RelationalDatabaseFacadeExtensions.ExecuteSqlCommand(context.Database, sprintf "SELECT * FROM mytable WHERE mycol=%O%O" x guid) // Noncompliant

        context.Database.ExecuteSqlCommandAsync(sprintf "") // Compliant, FormattableString is sanitized
        context.Database.ExecuteSqlCommandAsync("") // Compliant, constants are safe
        context.Database.ExecuteSqlCommandAsync(ConstQuery) // Compliant, constants are safe
        context.Database.ExecuteSqlCommandAsync("" + "") // Compliant, constants are safe
        context.Database.ExecuteSqlCommandAsync(query) // Compliant, not concat
        context.Database.ExecuteSqlCommandAsync("" + query) // Noncompliant
        context.Database.ExecuteSqlCommandAsync(query + "") // Noncompliant
        context.Database.ExecuteSqlCommandAsync("" + query + "") // Noncompliant
        context.Database.ExecuteSqlCommandAsync(sprintf "%O" parameters) // Noncompliant FP, interpolated string
        context.Database.ExecuteSqlCommandAsync(query, parameters) // Compliant, not concat or format
        context.Database.ExecuteSqlCommandAsync("" + query, parameters) // Noncompliant
        RelationalDatabaseFacadeExtensions.ExecuteSqlCommandAsync(context.Database, "" + query, parameters)  // Noncompliant

        context.Query<User>().FromSql(sprintf "") // Compliant, FormattableString is sanitized
        context.Query<User>().FromSql("") // Compliant, constants are safe
        context.Query<User>().FromSql(ConstQuery) // Compliant, constants are safe
        context.Query<User>().FromSql(query) // Compliant, not concat/format
        context.Query<User>().FromSql("" + "") // Compliant
        context.Query<User>().FromSql(sprintf "%O" parameters) // Noncompliant FP, interpolated string with argument tranformed in RawQuery
        context.Query<User>().FromSql("", parameters) // Compliant, the parameters are sanitized
        context.Query<User>().FromSql(query, parameters) // Compliant
        context.Query<User>().FromSql("" + query, parameters) // Noncompliant
        RelationalQueryableExtensions.FromSql(context.Query<User>(), "" + query, parameters) // Noncompliant


    member this.ConcatAndFormat(context:DbContext, query:string, [<ParamArray>] parameters:obj[]) =
        context.Database.ExecuteSqlCommand(String.Concat(query, parameters)) // Noncompliant
        context.Database.ExecuteSqlCommand(String.Format(query, parameters)) // Noncompliant
        context.Database.ExecuteSqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", parameters)) // Noncompliant
        //context.Database.ExecuteSqlCommand(sprintf "SELECT * FROM mytable WHERE mycol={parameters[0]}") // Compliant, the FormattableString is transformed into a parametrized query.
        context.Database.ExecuteSqlCommand("SELECT * FROM mytable WHERE mycol=" + string parameters.[0]) // Noncompliant
        let formatted = String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", parameters)
        context.Database.ExecuteSqlCommand(formatted) // FN

        context.Database.ExecuteSqlCommandAsync(String.Concat(query, parameters)) // Noncompliant
        context.Database.ExecuteSqlCommandAsync(String.Format(query, parameters)) // Noncompliant
        context.Database.ExecuteSqlCommandAsync(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", parameters)) // Noncompliant
        //context.Database.ExecuteSqlCommandAsync(sprintf "SELECT * FROM mytable WHERE mycol={parameters[0]}") // Compliant, the FormattableString is transformed into a parametrized query.
        context.Database.ExecuteSqlCommandAsync("SELECT * FROM mytable WHERE mycol=" + string parameters.[0]) // Noncompliant
        let concatenated = String.Concat(query, parameters)
        context.Database.ExecuteSqlCommandAsync(concatenated) // FN

        context.Query<User>().FromSql(String.Concat(query, parameters)) // Noncompliant
        context.Query<User>().FromSql(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", parameters)) // Noncompliant
        //context.Query<User>().FromSql(sprintf "SELECT * FROM mytable WHERE mycol={parameters[0]}") // Compliant, the FormattableString is transformed into a parametrized query.
        context.Query<User>().FromSql("SELECT * FROM mytable WHERE mycol=" + string parameters.[0]) // Noncompliant
        //var interpolated = sprintf "SELECT * FROM mytable WHERE mycol={parameters[0]}"
        //context.Query<User>().FromSql(interpolated) // FN


// ======================================================
// Code used in Net46
// ======================================================

type Net46Program() =
    let ConstantQuery = ""

    member this.CompliantSqlCommands(connection:DbConnection, transaction:DbTransaction, query:string) =
        let command = SqlCommand() // Compliant
        let command = SqlCommand("") // Compliant
        let command = SqlCommand(ConstantQuery) // Compliant
        let command = SqlCommand(query) // Compliant, we don't know anything about the parameter
        let command = SqlCommand(query, connection) // Compliant
        let command = SqlCommand("", connection) // Compliant, constant queries are safe
        let command = SqlCommand(query, connection, transaction) // Compliant
        let command = SqlCommand("", connection, transaction) // Compliant, constant queries are safe
        let command = SqlCommand(query, connection, transaction, SqlCommandColumnEncryptionSetting.Enabled) // Compliant
        let command = SqlCommand("", connection, transaction, SqlCommandColumnEncryptionSetting.Enabled) // Compliant, constant queries are safe

        command.CommandText <- query // Compliant, we don't know enough about the parameter
        command.CommandText <- ConstantQuery // Compliant
        let text = command.CommandText // Compliant
        let text = command.CommandText <- query // Compliant

        let adapter = SqlDataAdapter() // Compliant
        let adapter = SqlDataAdapter(command) // Compliant
        let adapter = SqlDataAdapter(query, "") // Compliant
        let adapter = SqlDataAdapter(query, connection) // Compliant
        ()

    member this.NonCompliant_Concat_SqlCommands(connection:DbConnection, transaction:DbTransaction, query:string, param:string) =
        let command = SqlCommand(String.Concat(query, param)) // Noncompliant {{Make sure that executing SQL queries is safe here.}}
//                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        let command = SqlCommand(query + param, connection) // Noncompliant
        let command = SqlCommand("" + string 1 + string 2, connection) // Compliant
        let command = SqlCommand(String.Concat(query, param), connection) // Noncompliant
        let command = SqlCommand(String.Concat(query, param), connection, transaction) // Noncompliant
        let command = SqlCommand(String.Concat(query, param), connection, transaction, SqlCommandColumnEncryptionSetting.Enabled) // Noncompliant

        command.CommandText <- String.Concat(query, param) // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^
        command.CommandText <- String.Concat(query, param) // Noncompliant

        let adapter = SqlDataAdapter(String.Concat(query, param), "") // Noncompliant
        ()

    member this.NonCompliant_Format_SqlCommands(connection:DbConnection, transaction:DbTransaction, param:string) =
        let command = SqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param)) // Noncompliant {{Make sure that executing SQL queries is safe here.}}
//                        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        let command = SqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param), connection) // Noncompliant
        let command = SqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param), connection, transaction) // Noncompliant
        let command = SqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param), connection, transaction, SqlCommandColumnEncryptionSetting.Enabled) // Noncompliant
        let x = 1
        let  g = Guid.NewGuid()
        let dateTime = DateTime.Now
        let command = SqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\", \"{2}\", \"{3}\")", x, g, dateTime), // Noncompliant - scalars can be dangerous and lead to expensive queries
                                 connection, transaction, SqlCommandColumnEncryptionSetting.Enabled)
        let command = SqlCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\", \"{2}\", \"{3}\")", x, param, dateTime), // Noncompliant
                                 connection, transaction, SqlCommandColumnEncryptionSetting.Enabled)

        command.CommandText <- String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param) // Noncompliant

        let adapter = SqlDataAdapter(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param), "") // Noncompliant
        ()


    member this.NonCompliant_Interpolation_SqlCommands(connection:DbConnection, transaction:DbTransaction, param:string) =
        let command = SqlCommand(sprintf "SELECT * FROM mytable WHERE mycol=%O" param) // Noncompliant {{Make sure that executing SQL queries is safe here.}}
//                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        let command = SqlCommand(sprintf "SELECT * FROM mytable WHERE mycol=%O" param, connection, transaction, SqlCommandColumnEncryptionSetting.Enabled) // Noncompliant

        command.CommandText <- sprintf "SELECT * FROM mytable WHERE mycol=%O " param  // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^

        let adapter = SqlDataAdapter(sprintf "SELECT * FROM mytable WHERE mycol=%O" param, "") // Noncompliant
        ()

    member this.OdbcCommands(connection:DbConnection, transaction:DbTransaction, query:string) =
        let command = OdbcCommand() // Compliant
        let command = OdbcCommand("") // Compliant
        let command = OdbcCommand(ConstantQuery) // Compliant
        let command = OdbcCommand(query) // Compliant, we don't know anything about the parameter
        let command = OdbcCommand(query, connection) // Compliant
        let command = OdbcCommand(query, connection, transaction) // Compliant

        command.CommandText <- query // Compliant
        command.CommandText <- ConstantQuery // Compliant
        let text = command.CommandText // Compliant
        let text = command.CommandText <- query // Compliant

        let adapter = OdbcDataAdapter() // Compliant
        let adapter = OdbcDataAdapter(command) // Compliant
        let adapter = OdbcDataAdapter(query, "") // Compliant
        let adapter = OdbcDataAdapter(query, connection) // Compliant
        ()

(*
For the rest of the frameworks, we do sparse testing, to keep tests maintainable and relevant
*)

    member this.NonCompliant_OdbcCommands(connection:DbConnection, transaction:DbTransaction, query:string, param:string) =
        let command = OdbcCommand(String.Concat(query, param)) // Noncompliant {{Make sure that executing SQL queries is safe here.}}
        command.CommandText <- String.Concat(query, param) // Noncompliant
        command.CommandText <- sprintf "SELECT * FROM mytable WHERE mycol=%O" param // Noncompliant
        let adapter = OdbcDataAdapter(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param), "") // Noncompliant
        ()

    member this.OracleCommands(connection:DbConnection, transaction:DbTransaction, query:string) =
        let command = OracleCommand() // Compliant
        let command = OracleCommand("") // Compliant
        let command = OracleCommand(ConstantQuery) // Compliant
        let command = OracleCommand(query) // Compliant, we don't know anything about the parameter
        let command = OracleCommand(query, connection) // Compliant, we don't know anything about the parameter
        let command = OracleCommand(query, connection, transaction) // Compliant, we don't know anything about the parameter

        command.CommandText <- query // Compliant, we don't know anything about the parameter
        command.CommandText <- ConstantQuery // Compliant
        let text = command.CommandText // Compliant
        let text = command.CommandText <- query // Compliant, we don't know anything about the parameter

        let adapter = OracleDataAdapter() // Compliant
        let adapter = OracleDataAdapter(command) // Compliant
        let adapter = OracleDataAdapter(query, "") // Compliant, we don't know anything about the parameter
        let adapter = OracleDataAdapter(query, connection) // Compliant, we don't know anything about the parameter
        ()

    member this.NonCompliant_OracleCommands(connection:DbConnection, transaction:DbTransaction, query:string, param:string) =
        let command = OracleCommand(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param)) // Noncompliant {{Make sure that executing SQL queries is safe here.}}
        command.CommandText <- sprintf "SELECT * FROM mytable WHERE mycol=%O" param // Noncompliant
        let adapter = OracleDataAdapter(String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param), "") // Noncompliant
        ()

    member this.SqlServerCeCommands(connection:DbConnection, transaction:DbTransaction, query:string) =
        let command = SqlCeCommand() // Compliant
        let command = SqlCeCommand("") // Compliant
        let command = SqlCeCommand(ConstantQuery) // Compliant
        let command = SqlCeCommand(query) // Compliant
        let command = SqlCeCommand(query, connection) // Compliant
        let command = SqlCeCommand(query, connection, transaction) // Compliant

        command.CommandText <- query // Compliant
        command.CommandText <- ConstantQuery // Compliant
        let text = command.CommandText // Compliant
        let text = command.CommandText = query // Compliant

        let adapter = SqlCeDataAdapter() // Compliant
        let adapter = SqlCeDataAdapter(command) // Compliant
        let adapter = SqlCeDataAdapter(query, "") // Compliant
        let adapter = SqlCeDataAdapter(query, connection) // Compliant
        ()

    member this.NonCompliant_SqlCeCommands(connection:DbConnection, transaction:DbTransaction, query:string, param:string) =
        let adapter = SqlCeDataAdapter(String.Concat(query, param), "") // Noncompliant
        let command = SqlCeCommand(sprintf "SELECT * FROM mytable WHERE mycol=%O" param) // Noncompliant {{Make sure that executing SQL queries is safe here.}}
        command.CommandText <- String.Format("INSERT INTO Users (name) VALUES (\"{0}\")", param) // Noncompliant
        ()
