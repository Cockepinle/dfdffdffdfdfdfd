using Serveeer.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Serveeer.Model
{
    public class TcpServer
    {
        public Socket socket; 
        public List<Socket> clients = new List<Socket>();
        private ServerViewModel viewModel;
        private MainViewModel mainView;
        public ObservableCollection<string> backUp = new ObservableCollection<string>();
        public List<User> users = new List<User>();

        public TcpServer(ServerViewModel viewModel, MainViewModel mainView)
        {
            this.viewModel = viewModel;
            this.mainView = mainView;

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen(1000);
            ListenToClients();
        }
        private async Task ListenToClients()
        {
            while (true)
            {
                var client = await socket.AcceptAsync();
                clients.Add(client);

                ReceiveMessage(client);
            }
        }
        private async Task ReceiveMessage(Socket client)
        {
            bool isNewUser = true;
            string receivedMessage = "";

            while (true)
            {
                byte[] bytes = new byte[1024];
                int bytesRead = await client.ReceiveAsync(bytes, SocketFlags.None);
                string text = Encoding.Unicode.GetString(bytes, 0, bytesRead);

                receivedMessage += text;

                if (isNewUser)
                {
                    User newUser = new User();
                    newUser.socket = client;
                    newUser.name = receivedMessage;
                    newUser.logTime = DateTime.Now;
                    users.Add(newUser);
                    viewModel.UserList.Add(receivedMessage);
                    backUp.Add($"{newUser.name} - подключился\n {DateTime.Now}");
                    string names = "/log\n" + string.Join("\n", viewModel.UserList);
                    foreach (User user in users)
                    {
                        SendMessage(user.socket, names);                       
                    }

                    isNewUser = false;
                }
                else
                {
                    string messageText = receivedMessage;
                    if (messageText.StartsWith("/log"))
                    {
                        viewModel.UserList.Clear();
                        viewModel.UserList = new ObservableCollection<string>(messageText.Split('\n'));
                        viewModel.UserList.RemoveAt(0);

                    }
                    else
                    {
                        string messageSender = users.FirstOrDefault(u => u.socket == client)?.name;
                        string messageSendDate = DateTime.Now.ToString();
                        string fulltext = $"[ {messageSendDate} ] {messageSender}: {messageText}";


                        foreach (User user in users)
                        {
                            if (user.socket != client)
                            {
                                SendMessage(user.socket, fulltext);
                            }
                        }
                        viewModel.MessageList.Add(fulltext);
                        SendMessage(client, fulltext);
                        fulltext = "";
                    }

                }
                receivedMessage = ""; // Сбрасываем сообщение после обработки
            }
        }
        public async Task SendMessage(Socket client, string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            await client.SendAsync(bytes, SocketFlags.None);
        }
    }
}
