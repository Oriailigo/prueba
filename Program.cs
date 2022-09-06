// See https://aka.ms/new-console-template for more information
using MySql.Data.MySqlClient;  //problemas con las dependencias
using System.Diagnostics;
using System.Net;
//traer clases externas
namespace MyApp
{
    class Program
    {         
        static void Main(string[] args){
            /*
            *********************************************************************
            * NOTA: Surgio problemas al momento de insertar los datos
            en la otra tabla. Hay problemas de tipo pero se supo solucionar.
            *********************************************************************
            */
            int periodo=determinarPeriodoMSH();            
            while(true){
                    Thread.Sleep(periodo);//espero un tiempo determinado por periodo
                    try
                    {
                        //conectando BD
                        MySql.Data.MySqlClient.MySqlConnection conn= ConexionBD();
                        string sql = "SELECT * FROM schedule";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        MySqlDataReader rdr = cmd.ExecuteReader();
                
                        //crear una lista para almacenar los datos de la BD
                        List<Schedule> list= almacenarLista(rdr);
                        
                        conn.Close();//cerrar conexion

                        // recorro la lista
                        foreach (var item in list)
                        {
                            //Console.WriteLine("esto tiene==="+item.getAction());
                            DateTime datetime_sistema=determinarDateTimeSistema();
                            
                            int tiempoComparado=DateTime.Compare(item.getExecutiondate(),datetime_sistema); 
                            //Console.WriteLine("tiempocomp== "+tiempoComparado);
                             if((item.getExecution()==1)&&(tiempoComparado==0)){ // si la accion no fue ejecutada y si es el momento para ejecutar la accion.
                                     item.setExecution(0);// se ejecuto
                                     //añadir cambio en la tabla schedule

                                        modificarBD_Schedule(item.getId());

                                    //  Console.WriteLine("se cambio executio=  "+item.getExecution());
                                     if(item.getAction()=="API"){
                                        ejecutarAPI(item);//ejecuto la accion de API.
                                     }
                                     else{
                                        ejecutarCMD(item);//ejecuto la accion comando de linux.  

                                     }

                             }
                        }
                        Console.WriteLine("Se pudo conectar a la BD");

                    }
                    catch (MySql.Data.MySqlClient.MySqlException ex)
                    {
                        Console.WriteLine("no se pudo conectar a la BD");
                        
                    }
                    
             }

        }

        //funciones
        /*
        ***************************************************
        *** Funcion que almacena los datos de la tabla
        *** schedule en una lista de tipo schedule
        *** y retorna la lista con los datos cargados.  
        ***************************************************
        */
        static List<Schedule> almacenarLista(MySqlDataReader rdr){
            List<Schedule> list = new List<Schedule>();
            while (rdr.Read()) 
            {
                    list.Add(new Schedule()  
                        {  
                            action= rdr["action"].ToString(),
                            id= Convert.ToInt32(rdr["id"]),
                            actiondetail= rdr["actiondetail"].ToString(),
                            executiondate= Convert.ToDateTime(rdr["executiondate"]),
                            execution= Convert.ToInt32(rdr["execution"])

                        });  
                            
            }
            return list;
        }
        /*
        ***************************************************
        *** Funcion que realiza la conexion con la BD.  
        ***************************************************
        */
        static MySql.Data.MySqlClient.MySqlConnection ConexionBD(){
            //conectando BD
            string connectionString= "server=35.175.112.147; port=3306; uid=testuser;"+
                                                    "pwd=Test.344; database=test; charset=utf8; sslMode=none";
            MySql.Data.MySqlClient.MySqlConnection conn;
                    
            conn = new MySql.Data.MySqlClient.MySqlConnection();
            conn.ConnectionString = connectionString;
            conn.Open();
            return conn;
        }
        
        /*
        ***************************************************
        *** Funcion que ejecuta la accion de tipo API.  
        ***************************************************
        */
        static void ejecutarAPI(Schedule schedule){
            string html = string.Empty;
            string url= schedule.getActiondetail();
            // string url = @"https://api.stackexchange.com/2.2/answers?order=desc&sort=activity&site=stackoverflow";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()){
                int codig=(int)response.StatusCode;
                if(codig>=200 && codig<=299){
                    codig=1; //se ejecuto correcto
                }else{
                    if(codig>=400 && codig<=599){
                     
                        codig=0;
                    }
                }
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
                String actiondetail=html.Substring(0,100);
                // Console.WriteLine(html);
                // Console.WriteLine("este es el estado=   "+codig);
                //creo el objeto y le añado los datos
                agregarBD_Schedulelog(actiondetail,codig,schedule.getId());
                
            }
        }

        /***************************************************
        *** Funcion que modifica el campo execution de la 
        *** tabla schedule para que la tarea se ejecute una
        *** unica vez.  
        ***************************************************
        */
        static  void modificarBD_Schedule(long id){
            //conectando BD
            MySql.Data.MySqlClient.MySqlConnection conn= ConexionBD();
            MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE schedule SET execution = 0  WHERE id="+id+";";            
            cmd.ExecuteNonQuery();
            conn.Close(); // cerrar conexion
            // Console.WriteLine("hola entre a la funcion");
                
        }

        /***************************************************
        *** Funcion que accede a la tabla schedulelog
        *** una vez obtenido los datos correspondientes
        *** para insertarlos en la tabla schedulelog.  
        ***************************************************
        */
        static  void agregarBD_Schedulelog(string html,int codig, int idForaña){
            //conectando BD
            MySql.Data.MySqlClient.MySqlConnection conn= ConexionBD();
            MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO schedulelog(id,success,actionresult,executiondate,scheduleid) VALUES(?id,?success,?actionresult,?executiondate,?scheduleid)";            
            long id = cmd.LastInsertedId;
            cmd.Parameters.Add("?id",MySqlDbType.Int64).Value=id;
            cmd.Parameters.Add("?success",MySqlDbType.Byte).Value=codig;
            cmd.Parameters.Add("?actionresult",MySqlDbType.VarString).Value=html;
            cmd.Parameters.Add("?executiondate",MySqlDbType.DateTime).Value=DateTime.Now;
            cmd.Parameters.Add("?scheduleid",MySqlDbType.Int64).Value=idForaña;
            // cmd.CommandText=sql;
            cmd.ExecuteNonQuery();
            conn.Close();// cerrar conexion
            // Console.WriteLine("hola entre a la funcion");
                
        }

        /*
        ***************************************************
        *** Funcion que ejecuta la accion de tipo LINUX.  
        ***************************************************
        */
        static void ejecutarCMD(Schedule schedule){

                int success;
                string comando= schedule.getActiondetail();
                // string comando= "ipconfig"; //comando de prueba a ejecutar
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();

                cmd.StandardInput.WriteLine(comando);
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();
                string resultadoComando=cmd.StandardOutput.ReadToEnd().Substring(138,100); //campuro los 100 primeros caracteres
                Console.WriteLine(resultadoComando); //resultado del comando
                string actionresult=resultadoComando;
                if(resultadoComando.Length!=0){//valido si se realizo correctamente el comando
                    success=1;
                }
                else{
                    success=0;
                }
                //creo el objeto y le añado los datos
                Console.WriteLine("Estoy en la funcion CMD");
                agregarBD_Schedulelog(actionresult,success,schedule.getId());
               
        }


        /*
        ***************************************************
        *** Funcion que Determina el datetime del sistema
        ***************************************************
        */    
        static DateTime determinarDateTimeSistema(){
            DateTime datetime_sistema=DateTime.Now;
            return datetime_sistema;
        }

        /*
        ***************************************************
        *** Funcion que Determina el periodo ya sea en minutos
        *** segundos u horas.  
        ***************************************************
        */    
        static int determinarPeriodoMSH(){
            Console.WriteLine("Escriba el periodo de tiempo (h hora, s segundos, m minutos) que decea para leer la tabla schedule");
            Console.WriteLine("Por ejemplo: 5s");
            string dato= Console.ReadLine();
            string subnum= dato.Substring(0,dato.Length-1);
            int num= int.Parse(subnum);
            char unidadT=dato[dato.Length-1];
            
            switch (unidadT)
            {
                case 's': Console.WriteLine("Eligio segundos"); num= num*1000; break;
                case 'm': Console.WriteLine("Eligio minutos"); num= num*60*1000;  break;
                case 'h': Console.WriteLine("Eligio horas"); num= num*60*60*1000; break;
                default: Console.WriteLine("error: Valor incorrecto.");  break;
            }
            return num;
        }
    }
}