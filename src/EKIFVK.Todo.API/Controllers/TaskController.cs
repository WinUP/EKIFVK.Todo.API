﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EKIFVK.DeusLegem.CreationSystem.API;
using EKIFVK.Todo.API.Models;
using EKIFVK.Todo.API.Services;
using EKIFVK.Todo.API.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EKIFVK.Todo.API.Controllers
{
    [Route("task")]
    public class TaskController : UserBasedController
    {
        public TaskController(DatabaseContext database, IPermissionService checker, IOptions<SystemConsts> consts)
            : base(database, checker, consts) { }

        /// <summary>
        /// 获取任务信息<br />
        /// <br />
        /// 权限：无<br />
        /// 返回：200 SUCCESS -> name, description, deadline, finished[bool]<br />
        /// <list type="bullet">
        /// <item><description>Token或任务不存在：401 INVALID_NAME -> null</description></item>
        /// <item><description>当前会话用户不是目标任务创建者：403 PERMISSION_DENIED -> null</description></item>
        /// </list>
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public JsonResult GetInfo(int id)
        {
            var user = FindUser();
            if (user == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            Checker.UpdateAccessTime(user);
            var task = Database.Task.FirstOrDefault(e => e.Id == id);
            if (task == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            if (task.Owner != user.Id)
                return JsonResponse(StatusCodes.Status403Forbidden, Consts.Value.PERMISSION_DENIED);
            return JsonResponse(data: new Hashtable
            {
                {"name", task.Name},
                {"description", task.Description},
                {"deadline", task.Deadline?.ToString("yyyy-MM-dd HH:mm:ss")},
                {"finished", task.Finished}
            });
        }

        /// <summary>
        /// 添加新的任务<br />
        /// <br />
        /// 权限：无<br />
        /// 返回：200 SUCCESS -> id[int]<br />
        /// <list type="bullet">
        /// <item><description>任务名不合法：400 INVALID_NAME -> null</description></item>
        /// <item><description>Token不存在：401 INVALID_NAME -> null</description></item>
        /// </list>
        /// </summary>
        /// <param name="parameter">
        /// 来自Body的参数<br />
        /// <list type="bullet">
        /// <item><description>name: 任务名</description></item>
        /// <item><description>description: 任务描述</description></item>
        /// <item><description>deadline?: 任务期限</description></item>
        /// </list>
        /// </param>
        /// <returns></returns>
        [HttpPost()]
        public JsonResult Add([FromBody] Hashtable parameter)
        {
            var user = FindUser();
            if (user == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            Checker.UpdateAccessTime(user);
            var task = new Task
            {
                Name = parameter["name"].ToString(),
                Description = parameter["description"].ToString()
            };
            if (parameter.ContainsKey("deadline"))
                task.Deadline = DateTime.Parse(parameter["deadline"].ToString());
            task.OwnerNavigation = user;
            task.Finished = false;
            Database.Task.Add(task);
            Database.SaveChanges();
            return JsonResponse(data: task.Id);
        }

        /// <summary>
        /// 删除任务<br />
        /// <br />
        /// 权限：无<br />
        /// 返回：200 SUCCESS -> null<br />
        /// <list type="bullet">
        /// <item><description>Token或任务不存在：401 INVALID_NAME -> null</description></item>
        /// <item><description>当前会话用户不是目标任务创建者：403 PERMISSION_DENIED -> null</description></item>
        /// </list>
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public JsonResult Delete(int id)
        {
            var user = FindUser();
            if (user == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            Checker.UpdateAccessTime(user);
            var task = Database.Task.FirstOrDefault(e => e.Id == id);
            if (task == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            if (task.Owner != user.Id)
                return JsonResponse(StatusCodes.Status403Forbidden, Consts.Value.PERMISSION_DENIED);
            Database.Task.Remove(task);
            Database.SaveChanges();
            return JsonResponse();
        }

        /// <summary>
        /// 修改任务内容<br />
        /// <br />
        /// 权限：无<br />
        /// 返回：200 SUCCESS -> null<br />
        /// <list type="bullet">
        /// <item><description>无法识别的时间格式：400 INVALID_PARAM -> null</description></item>
        /// <item><description>Token或任务不存在，或使用了未知的操作类型：401 INVALID_NAME -> null</description></item>
        /// <item><description>当前会话用户不是目标任务创建者：403 PERMISSION_DENIED -> null</description></item>
        /// </list>
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="parameter">
        /// 来自Body的参数<br />
        /// <list type="bullet">
        /// <item><description>operation: 操作类型[name:description:deadline:finished]</description></item>
        /// <item><description>data: 操作数据</description></item>
        /// </list>
        /// </param>
        /// <returns></returns>
        [HttpPatch("{id}")]
        public JsonResult PatchTask(int id, [FromBody] Hashtable parameter)
        {
            var user = FindUser();
            if (user == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            Checker.UpdateAccessTime(user);
            var task = Database.Task.FirstOrDefault(e => e.Id == id);
            if (task == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            if (task.Owner != user.Id)
                return JsonResponse(StatusCodes.Status403Forbidden, Consts.Value.PERMISSION_DENIED);
            var operation = parameter["operation"].ToString();
            var data = parameter["data"];
            switch (operation)
            {
                case "name":
                    task.Name = (string)data;
                    break;
                case "description":
                    task.Description = (string) data;
                    break;
                case "deadline":
                    DateTime date;
                    if (!DateTime.TryParse((string) data, out date))
                        return JsonResponse(StatusCodes.Status400BadRequest, Consts.Value.INVALID_PARAM);
                    task.Deadline = date;
                    break;
                case "finished":
                    task.Finished = (bool)data;
                    break;
                default:
                    return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            }
            Database.SaveChanges();
            return JsonResponse();
        }

        /// <summary>
        /// 获取任务数量<br />
        /// <br />
        /// 权限：无<br />
        /// 返回：200 SUCCESS -> count[int]<br />
        /// <list type="bullet">
        /// <item><description>Token不存在：401 INVALID_NAME -> null</description></item>
        /// </list>
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns></returns>
        [HttpGet(".count")]
        public JsonResult GetTaskCount(TaskFilter filter)
        {
            var user = FindUser();
            if (user == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            Checker.UpdateAccessTime(user);
            return JsonResponse(data: ParseFilter(filter, user.Id).Count());
        }

        /// <summary>
        /// 获取任务名列表<br />
        /// <br />
        /// 权限：无<br />
        /// 返回：200 SUCCESS -> name[]<br />
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns></returns>
        [HttpGet(".list")]
        public JsonResult Search(TaskFilter filter)
        {
            var user = FindUser();
            if (user == null)
                return JsonResponse(StatusCodes.Status401Unauthorized, Consts.Value.INVALID_NAME);
            Checker.UpdateAccessTime(user);
            return
                JsonResponse(data: ParseFilter(filter, user.Id).Select(e => new {e.Id, e.Name, e.Description, e.Deadline, e.Finished}));
        }

        private IQueryable<Task> ParseFilter(TaskFilter filter, int userId)
        {
            var result = Database.Task.Where(e => e.Owner == userId);
            if (filter.OnlyFinished) result = result.Where(e => e.Finished);
            if (filter.OnlyUnfinish) result = result.Where(e => !e.Finished);
            if (filter.OnlyScheduled) result = result.Where(e => e.Deadline.HasValue);
            if (filter.OnlyNoSchedule) result = result.Where(e => !e.Deadline.HasValue);
            var count = filter.Count == 0 ? int.MaxValue : filter.Count;
            return result.OrderByDescending(e => e.Id).Skip(filter.Skip).Take(count);
        }
    }
}
