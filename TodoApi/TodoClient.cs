public class TodoClient(HttpClient httpClient) : ITodoClient
{
    public async Task<TodoItem?> GetTodoItem()
    {
        return await httpClient.GetFromJsonAsync<TodoItem>("https://dummyjson.com/todos");
    }
}
