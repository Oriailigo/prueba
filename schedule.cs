using System;
using System.Diagnostics;
using System.Net;

namespace MyApp
{
    public class Schedule
    {
        public string action="defecto";
        public int id=21;
        public string actiondetail="defecto";
        public DateTime executiondate=DateTime.Now;
        public int execution=1;
//get y set
        public int getId(){
            return this.id;
        }
        public void setId(int id){
            this.id=id;
        }
        public int getExecution(){
            return this.execution;
        }
        public void setExecution(int execution){
            this.execution=execution;
        }
        public DateTime getExecutiondate(){
            return this.executiondate;
        }
        public void setExecutiondate(DateTime executiondate){
            this.executiondate=executiondate;
        }

        public string getAction(){
            return this.action;
        }
        public void setAction(string action){
            this.action=action;
        }

        public string getActiondetail(){
            return this.actiondetail;
        }
        public void setActiondetail(string actiondetail){
            this.actiondetail=actiondetail;
        }
        public static void agregarDatoSchedlue()
        {
            Console.WriteLine("soy la clase schedule");
            // code here
        }
    }
}