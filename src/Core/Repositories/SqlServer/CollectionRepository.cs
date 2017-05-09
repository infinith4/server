﻿using System;
using Bit.Core.Models.Table;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Linq;
using Newtonsoft.Json;
using Bit.Core.Utilities;

namespace Bit.Core.Repositories.SqlServer
{
    public class CollectionRepository : Repository<Collection, Guid>, ICollectionRepository
    {
        public CollectionRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString)
        { }

        public CollectionRepository(string connectionString)
            : base(connectionString)
        { }

        public async Task<int> GetCountByOrganizationIdAsync(Guid organizationId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteScalarAsync<int>(
                    "[dbo].[Collection_ReadCountByOrganizationId]",
                    new { OrganizationId = organizationId },
                    commandType: CommandType.StoredProcedure);

                return results;
            }
        }

        public async Task<Tuple<Collection, ICollection<Guid>>> GetByIdWithGroupsAsync(Guid id)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryMultipleAsync(
                    $"[{Schema}].[Collection_ReadWithGroupsById]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                var collection = await results.ReadFirstOrDefaultAsync<Collection>();
                var groupIds = (await results.ReadAsync<Guid>()).ToList();

                return new Tuple<Collection, ICollection<Guid>>(collection, groupIds);
            }
        }

        public async Task<ICollection<Collection>> GetManyByOrganizationIdAsync(Guid organizationId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Collection>(
                    $"[{Schema}].[{Table}_ReadByOrganizationId]",
                    new { OrganizationId = organizationId },
                    commandType: CommandType.StoredProcedure);

                return results.ToList();
            }
        }

        public async Task<ICollection<Collection>> GetManyByUserIdAsync(Guid userId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Collection>(
                    $"[{Schema}].[{Table}_ReadByUserId]",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                return results.ToList();
            }
        }

        public async Task CreateAsync(Collection obj, IEnumerable<Guid> groupIds)
        {
            obj.SetNewId();
            var objWithGroups = JsonConvert.DeserializeObject<CollectionWithGroups>(JsonConvert.SerializeObject(obj));
            objWithGroups.GroupIds = groupIds.ToGuidIdArrayTVP();

            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteAsync(
                    $"[{Schema}].[Collection_CreateWithGroups]",
                    objWithGroups,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task ReplaceAsync(Collection obj, IEnumerable<Guid> groupIds)
        {
            var objWithGroups = JsonConvert.DeserializeObject<CollectionWithGroups>(JsonConvert.SerializeObject(obj));
            objWithGroups.GroupIds = groupIds.ToGuidIdArrayTVP();

            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteAsync(
                    $"[{Schema}].[Collection_UpdateWithGroups]",
                    objWithGroups,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public class CollectionWithGroups : Collection
        {
            public DataTable GroupIds { get; set; }
        }
    }
}
