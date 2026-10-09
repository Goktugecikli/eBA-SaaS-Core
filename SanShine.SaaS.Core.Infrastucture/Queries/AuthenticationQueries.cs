namespace SanShine.SaaS.Core.Infrastucture.Queries
{
    internal static class AuthenticationQueries
    {

        internal const string CreateRestApi = @"INSERT INTO api_users (username, password, status, company_id,company, branch_id,branch, role, responsible_user_name ,created_at, created_by)
                                                            VALUES (@username, @password, @status, @company_id ,@company, @branch_id,@branch, @role, @responsible_user_name, GETDATE(), @created_by) 
                                                SELECT * FROM api_users WHERE id = SCOPE_IDENTITY();";

        internal const string GetRestApiUser = @"SELECT id, username, password FROM api_users WHERE username = @username AND status = 'A'; ";

        internal const string SaveRefreshToken = @"UPDATE api_users SET refresh_token = @refresh_token WHERE username = @username ";

        internal const string GetUserRoleByUsername =@"select role from api_users where username=@username";

        internal const string GetUserByRefreshToken = @"select id, username, role,refresh_token, responsible_user_name from api_users  where refresh_token = @refresh_token;";
        //internal const string GetRestApiUserInfo = @"SELECT username, status, company, branch, created_at, created_by, updated_at, updated_by FROM api_users WHERE id = @id; ";

    }
}
