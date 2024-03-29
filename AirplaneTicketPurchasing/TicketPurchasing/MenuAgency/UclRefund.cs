﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketPurchasing.MenuAgency
{
    public partial class UclRefund : UserControl
    {
        private Database database = new Database();
        private Support support = new Support();
        private ButtonSeat button_seat = null;
        DataRow sch = null;
        DataGridViewRow row = null;
        bool isSelected = false;
        private DateTime deptDate = DateTime.Now;
        private int
            seatFirst = 0, seatBusiness = 0, seatEconomy = 0,
            colFirst = 0, colBusiness = 0, colEconomy = 0;

        #region Constructor
        public UclRefund()
        {
            InitializeComponent();
            defaultForm(false);
        }
        #endregion

        #region Method
        private void DrawGroupBox(GroupBox box, Graphics g, Color textColor, Color borderColor)
        {
            if (box != null)
            {
                Brush textBrush = new SolidBrush(textColor);
                Brush borderBrush = new SolidBrush(borderColor);
                Pen borderPen = new Pen(borderBrush);
                SizeF strSize = g.MeasureString(box.Text, box.Font);
                Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                               box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                               box.ClientRectangle.Width - 1,
                                               box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);
                g.Clear(this.BackColor);
                g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);
                g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
                g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
            }
        }

        public ButtonSeat buttonSeat
        {
            get { return button_seat; }set { button_seat = value; }
        }

        private void defaultForm(bool val)
        {
            flightGroup.Visible = val;
            customerGroup.Visible = val;
            finalGroup.Visible = val;
            isSelected = val;
            if(val == false)
                txtSearch.Clear();
        }
        #endregion

        #region Events
        private void groupBox3_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.White, Color.Gray);
        }
        #endregion

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if(!isSelected)
            {
                try
                {
                    DataSet data = database.getDataFromDatabase("sp_view_transaction_tickets",
                            new List<Parameter> { new Parameter("@bookingRef", txtSearch.Text) });
                    sch = data.Tables[0].Rows[0];

                    //load data to flight schedule group
                    lblAircraft.Text = sch["airName"].ToString();
                    lblArrivalCity.Text = sch["arrivalCity"].ToString();
                    lblDepartureCity.Text = sch["deptCity"].ToString();
                    lblDepartureDate.Text = sch.Field<DateTime>("deptDate").ToString("yyyy-MM-dd");
                    deptDate = new DateTime(sch.Field<DateTime>("deptDate").Year,
                        sch.Field<DateTime>("deptDate").Month,
                        sch.Field<DateTime>("deptDate").Day);
                    lblDepartureTime.Text = sch.Field<TimeSpan>("deptTime").ToString();
                    lblEconomyPrice.Text = "Rp. " + sch.Field<decimal>("EconomyPrice").ToString("N");
                    lblBusinessPrice.Text = "Rp. " + sch.Field<decimal>("BusinessPrice").ToString("N");
                    lblFirstPrice.Text = "Rp. " + sch.Field<decimal>("FirstPrice").ToString("N");
                    lblTotalPrice.Text = "Rp. " + sch.Field<decimal>("totalPrice").ToString("N");
                    string rawImage = sch["photo"].ToString();
                    string ext = rawImage.Substring(rawImage.IndexOf('/'),
                        rawImage.IndexOf(';') - rawImage.IndexOf('/'));
                    string dataImage = rawImage.Substring(rawImage.IndexOf(',') + 2,
                            rawImage.Length - (rawImage.IndexOf(',') + 2));
                    byte[] image = Convert.FromBase64String(dataImage);
                    photo.Image = support.byteArrayToImage(image);

                    //set visible group
                    defaultForm(true);

                    //load data to customer group
                    setSeat(sch["scheduleID"].ToString(), sch["aircraft"].ToString(), Decimal.Parse(sch["EconomyPrice"].ToString()),
                        Decimal.Parse(sch["BusinessPrice"].ToString()), Decimal.Parse(sch["FirstPrice"].ToString()));
                    refreshdgv();

                    //calculate refunded cash
                    refundAmount();
                }
                catch (Exception)
                {
                    MessageBox.Show("No Active Booking Reference Found", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void createTableCust()
        {
            dgvCust.Rows.Clear();
            dgvCust.Columns.Clear();
            dgvCust.Columns.Add("id", "ID");
            dgvCust.Columns.Add("name", "Name");
            dgvCust.Columns.Add("seat", "Seat Number");
            dgvCust.Columns.Add("cabin", "Cabin");
            dgvCust.Columns[0].Visible = false;
            dgvCust.ForeColor = Color.Black;
            dgvCust.HeaderBgColor = Color.Teal;
            dgvCust.HeaderForeColor = Color.White;
            dgvCust.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCust.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void refreshdgv()
        {
            createTableCust();
            DataSet data = database.getDataFromDatabase("sp_view_transaction_customer_seat",
                new List<Parameter> { new Parameter("@ticketID", sch["ticketID"].ToString()) });

            foreach (DataRow item in data.Tables[0].Rows)
            {
                string seatNumber = "";
                if(item["cabin"].ToString().Equals("First Class"))
                {
                    seatNumber = SeatNumber(seatFirst, colFirst, Convert.ToInt32(item["seat"].ToString()), item["cabin"].ToString());
                }
                else if(item["cabin"].ToString().Equals("Business Class"))
                {
                    seatNumber = SeatNumber(seatBusiness, colBusiness, Convert.ToInt32(item["seat"].ToString()), item["cabin"].ToString());
                }
                else
                {
                    seatNumber = SeatNumber(seatEconomy, colEconomy, Convert.ToInt32(item["seat"].ToString()), item["cabin"].ToString());
                }
                dgvCust.Rows.Add(item["name"].ToString(), item["name"].ToString(),
                    seatNumber, item["cabin"].ToString());
            }
        }

        private void dgvCust_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvCust.RowCount > 0)
                    row = dgvCust.Rows[e.RowIndex];
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string SeatNumber(int pos, int column, int seat,string cabintype)
        {
            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            double reduce = seat / column;
            int reducePos = (reduce.ToString().Split('.').Length > 2) ? Convert.ToInt32(reduce.ToString().Split('.')[0]) : (int)reduce;
            if (reducePos > 0) reducePos = reducePos - 1;
            double getPos = pos + (seat / column);
            int realSeat = (getPos.ToString().Split('.').Length > 2) ? Convert.ToInt32(getPos.ToString().Split('.')[0]) + 1 : (int)getPos;
            int numb = (seat % column) == 0 ? column : (seat % column);
            if(cabintype.Equals("Economy Class"))
                return alphabet[realSeat] + numb.ToString();
            else
                return alphabet[realSeat - reducePos] + numb.ToString();
        }

        private void setSeat(string sch, string aircraft,
            decimal economy, decimal business, decimal first)
        {
            double pos = 0;
            panelSeat.Controls.Clear();

            // First Class
            DataSet dsFirst = database.getDataFromDatabase("sp_view_transaction_get_seat",
               new List<Parameter> {
                    new Parameter("@AircraftID", aircraft),
                    new Parameter("@CabinType", "First Class")
               });

            if (dsFirst.Tables.Count > 0)
            {
                if (dsFirst.Tables[0].Rows.Count > 0)
                {
                    string cabintype = dsFirst.Tables[0].Rows[0][0].ToString();
                    int seat = Convert.ToInt32(dsFirst.Tables[0].Rows[0][1].ToString());
                    int left = Convert.ToInt32(dsFirst.Tables[0].Rows[0][2].ToString());
                    int mid = Convert.ToInt32(dsFirst.Tables[0].Rows[0][3].ToString());
                    int right = Convert.ToInt32(dsFirst.Tables[0].Rows[0][4].ToString());
                    UclSeatRefund seatFlight = new UclSeatRefund(sch, cabintype, left, mid, right, 
                        seat, (int)pos, first, null, this.sch["ticketID"].ToString(), 
                        txtSearch.Text);
                    pos = ((double)seat / (double)(left + mid + right));
                    colFirst = left + mid + right;
                    seatFlight.Margin = new Padding(0, 5, 0, 5);
                    panelSeat.Controls.Add(seatFlight);
                }
            }

            // Business Class
            DataSet dsBusiness = database.getDataFromDatabase("sp_view_transaction_get_seat",
                new List<Parameter> {
                    new Parameter("@AircraftID", aircraft),
                    new Parameter("@CabinType", "Business Class")
                });

            if (dsBusiness.Tables.Count > 0)
            {
                if (dsBusiness.Tables[0].Rows.Count > 0)
                {
                    string cabintype = dsBusiness.Tables[0].Rows[0][0].ToString();
                    int seat = Convert.ToInt32(dsBusiness.Tables[0].Rows[0][1].ToString());
                    int left = Convert.ToInt32(dsBusiness.Tables[0].Rows[0][2].ToString());
                    int mid = Convert.ToInt32(dsBusiness.Tables[0].Rows[0][3].ToString());
                    int right = Convert.ToInt32(dsBusiness.Tables[0].Rows[0][4].ToString());
                    if (pos.ToString().Split('.').Length == 2)
                    {
                        pos = Convert.ToDouble(pos.ToString().Split('.')[0]) + 1;
                        seatBusiness = (int)pos;
                    }
                    else
                    {
                        if(seatBusiness > 0)
                            seatBusiness = (int)pos + 1;
                        else
                            seatBusiness = (int)pos;
                    }

                    UclSeatRefund seatFlight = new UclSeatRefund(sch, cabintype, left, mid, 
                        right, seat, (int)pos, business, null, this.sch["ticketID"].ToString(), 
                        txtSearch.Text);
                    pos += (double)seat / (double)(left + mid + right);
                    colBusiness = left + mid + right;
                    seatFlight.Margin = new Padding(0, 5, 0, 5);
                    panelSeat.Controls.Add(seatFlight);
                }
            }

            // Economy Class
            DataSet dsEconomy = database.getDataFromDatabase("sp_view_transaction_get_seat",
                new List<Parameter> {
                    new Parameter("@AircraftID", aircraft),
                    new Parameter("@CabinType", "Economy Class")
                });

            if (dsEconomy.Tables.Count > 0)
            {
                if (dsEconomy.Tables[0].Rows.Count > 0)
                {
                    string cabintype = dsEconomy.Tables[0].Rows[0][0].ToString();
                    int seat = Convert.ToInt32(dsEconomy.Tables[0].Rows[0][1].ToString());
                    int left = Convert.ToInt32(dsEconomy.Tables[0].Rows[0][2].ToString());
                    int mid = Convert.ToInt32(dsEconomy.Tables[0].Rows[0][3].ToString());
                    int right = Convert.ToInt32(dsEconomy.Tables[0].Rows[0][4].ToString());
                    if (pos.ToString().Split('.').Length == 2)
                    {
                        pos = Convert.ToDouble(pos.ToString().Split('.')[0]) + 1;
                        seatEconomy = (int)pos;
                    }
                    else
                    {
                        if (seatEconomy > 0)
                            seatEconomy = (int)pos + 1;
                        else
                            seatEconomy = (int)pos;
                    }
                    UclSeatRefund seatFlight = new UclSeatRefund(sch, cabintype, left, mid, right, 
                        seat, (int)pos, economy, null, this.sch["ticketID"].ToString(), 
                        txtSearch.Text);
                    pos += (double)seat / (double)(left + mid + right);
                    colEconomy = left + mid + right;
                    seatFlight.Margin = new Padding(0, 5, 0, 5);
                    panelSeat.Controls.Add(seatFlight);
                }
            }
        }

        private void refundAmount()
        {
            DateTime date = (DateTime) sch["deptDate"];
            TimeSpan time = (TimeSpan) sch["deptTime"];
            DateTime deptDateTime = date.Add(time);
            DateTime currentTime = DateTime.Now;
            var hoursLeft = (deptDateTime - currentTime).TotalHours;
            decimal totalP = (decimal) sch["totalPrice"];
            float refundPercent = 1f;

            if (hoursLeft > 72) refundPercent = 0.75f;
            else if (hoursLeft > 48 && hoursLeft < 72) refundPercent = 0.5f;
            else if (hoursLeft > 24 && hoursLeft < 48) refundPercent = 0.4f;
            else if (hoursLeft > 12 && hoursLeft < 24) refundPercent = 0.3f;
            else if (hoursLeft > 4 && hoursLeft < 12) refundPercent = 0.2f;
            else if (hoursLeft > 0 && hoursLeft < 4) refundPercent = 0.1f;

            lblRefund.Text = "Rp. "+(totalP * (decimal)refundPercent).ToString("N");
        }

        private void btnRefund_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Are you sure?","Refund",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DateTime date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                if (deptDate < date)
                {
                    MessageBox.Show("Ensure departure date must bigger than today", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    int res = database.executeQuery("sp_update_tickets", new List<Parameter> {
                new Parameter ("@ticketID", this.sch["ticketID"].ToString()) }, "Refund");

                    if (res > 0)
                    {
                        MessageBox.Show("Tickets Successfuly Refunded!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        defaultForm(false);
                    }
                    else
                        MessageBox.Show("Refund ticket is failed", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            defaultForm(false);
            seatFirst = 0;
            seatBusiness = 0;
            seatEconomy = 0;
            colFirst = 0;
            colBusiness = 0;
            colEconomy = 0;
        }
    }
}
