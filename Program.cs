using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;

namespace cBot
{
    class SneezeData
    {
        Random rand = new();
        String[] sneezeGif = new String[]
        {
            "https://tenor.com/YPru.gif",
            "https://tenor.com/QoBR.gif",
            "https://tenor.com/232B.gif",
            "https://tenor.com/bmZgQ.gif",
            "https://tenor.com/7otQ.gif",
            "https://tenor.com/zaBp.gif",
            "https://tenor.com/zaBp.gif",
            "https://tenor.com/bDyoV.gif"
        };
        String[] sneezeLines = new string[]
        {
            "<b>BE CAREFUL! Someone just sneezed here!</b>",
            "<b>SNEEZE IN THE HOLE</b>, Everyone! Get cover.",
            "Oh No, Did someone just sneezed here?",
            "Guys its not safe here, Looks like someone just <b>sneezed</b>",
            "He Sneezed, Take him to the hospital!",
            "Not again!",
            "<i>Achhoooooooooooooooo</i>"
        };

        public string getSneezeGif()
        {
            int r = rand.Next(sneezeGif.Length);
            return sneezeGif[r];
        }
        public string getSneezeLine()
        {
            int r = rand.Next(sneezeLines.Length);
            return sneezeLines[r];
        }
    }

    class Program
    {
        public static async Task<bool> IsCollectionExistsAsync(string collectionName, IMongoClient database)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await database.GetDatabase("BotDatabase").ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }
        public static async Task<bool> IsUserExistsAsync(IMongoCollection<BsonDocument> collection, string _userID)
        {

            BsonValue value = _userID;
            var documents = await collection.Find(new BsonDocument()).ToListAsync();
            foreach (var document in documents)
            {
                if (document.ContainsValue(value))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task processUpdate(Update e, IMongoCollection<BsonDocument> groupCollec, BsonDocument groupData, BsonDocument userData)
        {
            SneezeData sneeze = new SneezeData();
            Random rand = new Random();

            string userID = e.Message.From.Id.ToString();
            string chatID = e.Message.Chat.Id.ToString();
            int corocash = rand.Next(1, 10);

            var Gfilter = Builders<BsonDocument>.Filter.Eq("userid", chatID);
            var userFilter = Builders<BsonDocument>.Filter.Eq("userid", userID);


            BsonArray array = groupData.GetValue("party").AsBsonArray;
            BsonValue isInfected = userData.GetValue("isInfected");

            int CoroCashDB = userData.GetValue("CoroCash").AsInt32;
            float r = rand.Next(0, 100);

            if (!array.Contains(e.Message.From.Id)) // checking leakerlist array
            {
                var update = Builders<BsonDocument>.Update.Push<long>("party", e.Message.From.Id);
                await groupCollec.UpdateOneAsync(Gfilter, update);
            }

            if (groupData.GetValue("isCoronaThere").ToBoolean()) // checking if corona zone is finished
            {
                BsonDateTime ZoneUntil = groupData.GetValue("ZoneUntil").AsBsonDateTime;
                int coroZoneFinish = ZoneUntil.CompareTo(DateTime.Now.ToUniversalTime());

                if (coroZoneFinish <= 0)
                {
                    var Gupdate = Builders<BsonDocument>.Update.Set("isCoronaThere", false);
                    await groupCollec.UpdateOneAsync(Gfilter, Gupdate);
                }
            }

            if (!groupData.GetValue("isCoronaThere").ToBoolean() && groupData.GetValue("isCoronaLeaked").ToBoolean())
            {
                if (r <= 10 && isInfected == true || r > 10 && r < 20 && isInfected == false) // starting corozone
                {
                    double corocashMultiplier = (rand.NextDouble() * 7.00f) + 3.50f;
                    int zoneUntillR = rand.Next(120, 300);
                    corocashMultiplier = Math.Round(corocashMultiplier + 1.00, 2, MidpointRounding.AwayFromZero);
                    DateTime zoneUntil = DateTime.Now.ToUniversalTime();
                    zoneUntil = zoneUntil.AddSeconds(zoneUntillR);

                    var CoronaThereupdate = Builders<BsonDocument>.Update.Set("isCoronaThere", true);
                    var multiplierUpdate = Builders<BsonDocument>.Update.Set("CoroCashMultiplier", corocashMultiplier);
                    var EndUpdate = Builders<BsonDocument>.Update.Set("ZoneUntil", zoneUntil);

                    await groupCollec.UpdateOneAsync(Gfilter, CoronaThereupdate);
                    await groupCollec.UpdateOneAsync(Gfilter, multiplierUpdate);
                    await groupCollec.UpdateOneAsync(Gfilter, EndUpdate);
                    var startMessage = await botClient.SendAnimationAsync(e.Message.Chat.Id, sneeze.getSneezeGif(), caption: sneeze.getSneezeLine() + "\nCoroZone is started, CoroCash multiplier has been set to " + corocashMultiplier + "x \nThe CoroZone will remain for " + zoneUntillR + " seconds", parseMode: ParseMode.Html);
                    await Task.Delay(6000);
                    await botClient.DeleteMessageAsync(chatID, startMessage.MessageId);
                }
            }

            BsonDateTime dateTime = userData.GetValue("lastMessage").AsBsonDateTime;
            DateTime time = dateTime.ToUniversalTime();
            time = time.AddSeconds(3.00);
            int canSend = time.CompareTo(DateTime.Now.ToUniversalTime());
            if (canSend <= 0) // every message cooldown
            {
                DateTime maskUntill = userData.GetValue("maskUnitll").ToUniversalTime();
                if (maskUntill.CompareTo(DateTime.Now.ToUniversalTime()) <= 0)
                {
                    var infectionRateUpdate = Builders<BsonDocument>.Update.Set("getInfectedRate", 40);
                    await groupCollec.UpdateOneAsync(userFilter, infectionRateUpdate);
                }
                DateTime vaccineUntill = userData.GetValue("vaccUntill").ToUniversalTime();
                if (vaccineUntill.CompareTo(DateTime.Now.ToUniversalTime()) <= 0)
                {
                    var dieRateUpdate = Builders<BsonDocument>.Update.Set("dieRate", 4.69);
                    await groupCollec.UpdateOneAsync(userFilter, dieRateUpdate);
                }

                if (groupData.GetValue("isCoronaThere").ToBoolean()) //checking for updating corocash when CoroZone is active
                {
                    double corocashMultiplier = groupData.GetValue("CoroCashMultiplier").ToDouble();
                    CoroCashDB = (int)(CoroCashDB + (corocash * corocashMultiplier));
                    var CashUpdate = Builders<BsonDocument>.Update.Set("CoroCash", CoroCashDB);
                    await groupCollec.UpdateOneAsync(userFilter, CashUpdate);

                    int getInfectRate = userData.GetValue("getInfectedRate").ToInt32();
                    int getInfect = rand.Next(0, 100);
                    if (getInfect > getInfectRate && !isInfected.ToBoolean()) // checking for infecting others
                    {
                        var InfectedTrueUpdate = Builders<BsonDocument>.Update.Set("isInfected", true);
                        await groupCollec.UpdateOneAsync(userFilter, InfectedTrueUpdate);

                        var recoveryMessageUpdate = Builders<BsonDocument>.Update.Set("recoveryMessage", DateTime.Now.ToUniversalTime());
                        var recoveryHoursUpdate = Builders<BsonDocument>.Update.Set("recoveryHours", (Double)rand.Next(45, 55));

                        await groupCollec.UpdateOneAsync(userFilter, recoveryMessageUpdate);
                        await groupCollec.UpdateOneAsync(userFilter, recoveryHoursUpdate);
                    }

                }
                else if (groupData.GetValue("isCoronaLeaked").ToBoolean()) //checking for updating corocash when CoroZone is NOT active
                {
                    CoroCashDB = (int)(CoroCashDB + corocash);
                    var CashUpdate = Builders<BsonDocument>.Update.Set("CoroCash", CoroCashDB);
                    await groupCollec.UpdateOneAsync(userFilter, CashUpdate);
                }
                var updateUserTime = Builders<BsonDocument>.Update.Set("lastMessage", DateTime.Now);
                await groupCollec.UpdateOneAsync(userFilter, updateUserTime); // Updating the last message userset for every Cooldown
            }

            BsonDateTime dieMessage = userData.GetValue("dieMessage").AsBsonDateTime;
            DateTime mrutyu = dieMessage.ToUniversalTime();
            mrutyu = mrutyu.AddHours(4); // this will check the message every 3 hours
            if (isInfected.ToBoolean() && mrutyu.CompareTo(DateTime.Now.ToUniversalTime()) <= 0) // checking if a user can die by infection
            {
                double userDieRate = userData.GetValue("dieRate").AsDouble;
                double dieRate = rand.NextDouble() * 100.00;
                dieRate = Math.Round(dieRate, 2, MidpointRounding.AwayFromZero);
                if (dieRate < userDieRate) // killing off the user
                {
                    var InfectedFalseUpdate = Builders<BsonDocument>.Update.Set("isInfected", false);
                    var dieCoroUpdate = Builders<BsonDocument>.Update.Set("CoroCash", 0);
                    await groupCollec.UpdateOneAsync(userFilter, dieCoroUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, InfectedFalseUpdate);
                    await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/anime-die-dies-nekodies-nekodie-gif-21965693", caption: $"[{e.Message.From.FirstName}](tg://user?id={e.Message.From.Id}) has died due to Coronya!\nTry not to die next time. If you are negative buy a mask and keep the coronya away, If you think you are positive buy and use Vaccine or Meds\nRest in peace for now...", parseMode: ParseMode.Markdown);
                }
                else if (dieRate < 20)
                {
                    await botClient.SendTextMessageAsync(chatID, "Quick Reminder:\nYour Health condition doesnt seem too good, Consider buying vaccine and Meds!", replyToMessageId: e.Message.MessageId);
                }
                else
                {
                    var DieMessageUpdate = Builders<BsonDocument>.Update.Set("dieMessage", DateTime.Now.ToUniversalTime());
                    await groupCollec.UpdateOneAsync(userFilter, DieMessageUpdate);
                }
            }

            if (isInfected.AsBoolean) // checking if infected to recover
            {
                Double recoveryHours = userData.GetValue("recoveryHours").AsDouble;
                DateTime recoverMessage = userData.GetValue("recoveryMessage").ToUniversalTime();
                recoverMessage = recoverMessage.AddHours(recoveryHours);

                if (recoverMessage.CompareTo(DateTime.Now.ToUniversalTime()) <= 0) // checking if recover time has passed
                {
                    var InfectedFalseUpdate = Builders<BsonDocument>.Update.Set("isInfected", false);
                    await groupCollec.UpdateOneAsync(userFilter, InfectedFalseUpdate);
                }
            }

        }

        public static async Task LeakersList(Update e, IMongoCollection<BsonDocument> groupCollec)
        {
            var Gfilter = Builders<BsonDocument>.Filter.Eq("userid", e.Message.Chat.Id.ToString());
            BsonDocument doc = await groupCollec.Find(Gfilter).FirstAsync();


            BsonArray array = doc.GetValue("party").AsBsonArray;
            string members = $"Joined leakers:\n";
            BsonValue[] finalparty = array.ToArray();

            for (int i = 0; i < finalparty.Length; i++) // drawing leakerlist from array to int
            {
                try
                {
                    ChatMember user = await botClient.GetChatMemberAsync(e.Message.Chat.Id, (long)finalparty[i]);
                    string firstname = user.User.FirstName;
                    members = members + $"[{firstname}](tg://user?id={user.User.Id})" + ", \n";
                }
                catch (Exception exc)
                {
                    if (exc.Message == "Bad Request: user not found")
                    {
                        continue;
                    }
                }
            }

            members = members.Remove(members.Length - 3); // removing ',' for last person in list

            await botClient.SendTextMessageAsync(e.Message.Chat.Id, members, replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);

        }

        public static async Task Release(Update e, IMongoCollection<BsonDocument> groupCollec)
        {
            Random rand = new();
            var Gfilter = Builders<BsonDocument>.Filter.Eq("userid", e.Message.Chat.Id.ToString());
            BsonDocument doc = await groupCollec.Find(Gfilter).FirstAsync();

            BsonBoolean isCoronaLeaked = doc.GetValue("isCoronaLeaked").AsBoolean;

            if (isCoronaLeaked.ToBoolean()) // send this message when virus is already leaked
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Coronya has already been leaked in this group, You can't leak it twice!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);
                return;
            }

            BsonArray array = doc.GetValue("party").AsBsonArray;
            if (array.Count <= 4)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Need Atleast 4 leakers to leak the virus!\n\nTip: If not recommended wait time is around 2 days.\n\nAnyone who sends message after bot was added to the group are added in leakers list.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);
                return;
            }
            BsonValue[] finalparty = array.ToArray();

            int r = rand.Next(0, finalparty.Length);
            BsonValue infectedUserid = finalparty[r];

            var filter = Builders<BsonDocument>.Filter.Eq("userid", infectedUserid.ToString());
            var update = Builders<BsonDocument>.Update.Set("isInfected", true);
            var recoveryMessageUpdate = Builders<BsonDocument>.Update.Set("recoveryMessage", DateTime.Now.ToUniversalTime());
            var recoveryHoursUpdate = Builders<BsonDocument>.Update.Set("recoveryHours", (Double)rand.Next(45, 55));
            var CoroLeakedTrueupdate = Builders<BsonDocument>.Update.Set("isCoronaLeaked", true);

            await groupCollec.UpdateOneAsync(filter, update); //infected update
            await groupCollec.UpdateOneAsync(Gfilter, CoroLeakedTrueupdate); //coronaleakUpdate
            await groupCollec.UpdateOneAsync(filter, recoveryMessageUpdate); // recoverMessageUpdate
            await groupCollec.UpdateManyAsync(filter, recoveryHoursUpdate);

            /*await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Virus Released!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Markdown);*/
            await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/boom-gif-5722488", caption: "BEWARE! Virus has been leaked by" + e.Message.From.FirstName + "\nSomeone in the groups is coronya positive, Be careful or You might get infected.");
        }
        public static async Task ProfileAsync(Update e, IMongoCollection<BsonDocument> groupCollec, BsonDocument userData)
        {
            int corocash = userData.GetValue("CoroCash").ToInt32();
            int infectionRate = userData.GetValue("getInfectedRate").ToInt32();
            int rank = userData.GetValue("rank").ToInt32();
            string tested = userData.GetValue("tested").AsString;
            string Mask = "None";
            switch (infectionRate)
            {
                case 25:
                    Mask = "Disposable Mask😷";
                    break;
                case 15:
                    Mask = "Cotton Mask😷";
                    break;
                case 7:
                    Mask = "Expensive Mask😷";
                    break;
                default:
                    Mask = "None";
                    break;
            }

            try
            {
                //getting user profile picture
                UserProfilePhotos photo = await botClient.GetUserProfilePhotosAsync(e.Message.From.Id);
                string fileid = photo.Photos[0][0].FileId;
                File pfp = await botClient.GetFileAsync(fileid);

                await botClient.SendPhotoAsync
                    (
                        e.Message.Chat.Id,
                        pfp.FileId,
                        $"👤 Name : {e.Message.From.FirstName + e.Message.From.LastName}\n" +
                        $"🆔 User Id : <code>{e.Message.From.Id}</code>\n\n" +
                        $"💸 Corocash : {corocash}\n" +
                        $"🦠 Test Results : <code>{tested}</code>\n\n" +
                        $"🔼 Group Rank: {rank}\n" +
                        $"😷 Infection Rate: {infectionRate}%\n\n" +
                        $"👺Mask: {Mask}",
                        parseMode: ParseMode.Html
                    );
            }
            catch (Exception ex)
            {
                if (ex.Message == "Index was outside the bounds of the array.")
                {
                    await botClient.SendPhotoAsync
                    (
                        e.Message.Chat.Id,
                        $"👤 Name : {e.Message.From.FirstName + e.Message.From.LastName}\n" +
                        $"🆔 User Id : <code>{e.Message.From.Id}</code>\n\n" +
                        $"💸 Corocash : {corocash}\n" +
                        $"🦠 Test Results : <code>{tested}</code>\n\n" +
                        $"🔼 Group Rank: {rank}\n" +
                        $"😷 Infection Rate: {infectionRate}%\n\n" +
                        $"👺Mask: {Mask}",
                        parseMode: ParseMode.Html
                    );
                }
            }



        }
        public static async Task LeaderBoardAsync(Update e, IMongoCollection<BsonDocument> groupCollec)
        {
            var Filter = Builders<BsonDocument>.Filter.Eq("isUser", true);
            /*var sort = Builders<BsonDocument>.Sort.Descending("CoroCash");*/
            /*var docs = await groupCollec.Find(Filter).Sort(sort).ToListAsync();*/
            var unsDocs = await groupCollec.Find(Filter).ToListAsync();

            Dictionary<long, int> sortedLeaderboard = new Dictionary<long, int>();
            string leaderBoard = $"-<b>{e.Message.Chat.Title} Leaderboard</b>-\n\n";
            int i = 1;

            foreach (var sdoc in unsDocs)
            {
                long userID = (long)sdoc.GetValue("useridLong");
                int CoyroCash = sdoc.GetValue("CoroCash").AsInt32;

                sortedLeaderboard.Add(userID, CoyroCash);
            }
            foreach (KeyValuePair<long, int> leaderboard in sortedLeaderboard.OrderByDescending(key => key.Value))
            {
                try
                {
                    ChatMember user = await botClient.GetChatMemberAsync(e.Message.Chat.Id, leaderboard.Key);

                    if (i < 6)
                    {

                        //switching rank string
                        switch (i)
                        {
                            case 1:
                                leaderBoard = leaderBoard + $"🟡 |🥇 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + leaderboard.Value + "💸 \n";
                                break;
                            case 2:
                                leaderBoard = leaderBoard + $"🟡 |🥈 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + leaderboard.Value + "💸 \n";
                                break;
                            case 3:
                                leaderBoard = leaderBoard + $"🟡 |🥉 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + leaderboard.Value + "💸 \n";
                                break;
                            case 4:
                                leaderBoard = leaderBoard + $"🟣 | 4 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + leaderboard.Value + "💸 \n";

                                break;
                            case 5:
                                leaderBoard = leaderBoard + $"🟣 | 5 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + leaderboard.Value + "💸 \n";

                                break;
                        }
                        // storing rank in database
                        var filter = Builders<BsonDocument>.Filter.Eq("useridLong", leaderboard.Key);
                        var update = Builders<BsonDocument>.Update.Set("rank", i);
                        await groupCollec.UpdateOneAsync(filter, update);
                        i++;
                    }
                    else if (i < 21)
                    {
                        leaderBoard = leaderBoard + $"🟢 | {i} - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + leaderboard.Value + "💸 \n";
                        var filter = Builders<BsonDocument>.Filter.Eq("useridLong", leaderboard.Key);
                        var update = Builders<BsonDocument>.Update.Set("rank", i);
                        await groupCollec.UpdateOneAsync(filter, update);
                        i++;
                    }
                    else
                    {
                        //stored rank for rest user who werent in leaderboard
                        var filter = Builders<BsonDocument>.Filter.Eq("useridLong", leaderboard.Key);
                        var update = Builders<BsonDocument>.Update.Set("rank", i);
                        await groupCollec.UpdateOneAsync(filter, update);
                        i++;
                    }
                }
                catch (Exception exc)
                {
                    if (exc.Message != "Bad Request: user not found")
                    {
                        continue;
                    }
                }



            }
            //Alternative sorting method
            //yes
            /*foreach (var doc in docs)
            {
                ChatMember user = await botClient.GetChatMemberAsync(e.Message.Chat.Id, (long)doc.GetValue("useridLong"));
                // Display leaderboard for first 20 users
                if (i<6)
                {
                    
                    //switching rank string
                    switch (i)
                    {
                        case 1:
                            leaderBoard = leaderBoard + $"🟠 |🥇 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + doc.GetValue("CoroCash") + "💸 \n";
                            break;
                        case 2:
                            leaderBoard = leaderBoard + $"🟡 |🥈 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + doc.GetValue("CoroCash") + "💸 \n";                           
                            break;
                        case 3:
                            leaderBoard = leaderBoard + $"🟡 |🥉 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + doc.GetValue("CoroCash") + "💸 \n";
                            break;
                        case 4:
                            leaderBoard = leaderBoard + $"🟣 | 4 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + doc.GetValue("CoroCash") + "💸 \n";

                            break;
                        case 5:
                            leaderBoard = leaderBoard + $"🟣 | 5 - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + doc.GetValue("CoroCash") + "💸 \n";

                            break;
                    }
                    // storing rank in database
                    var filter = Builders<BsonDocument>.Filter.Eq("useridLong", (long)doc.GetValue("useridLong"));
                    var update = Builders<BsonDocument>.Update.Set("rank", i);
                    await groupCollec.UpdateOneAsync(filter, update);
                    i++;
                }
                else if (i < 21)
                {
                    leaderBoard = leaderBoard + $"🟢 | {i} - <a href = \"https://t.me/{user.User.Username}\">{user.User.FirstName}</a> - " + doc.GetValue("CoroCash") + "💸 \n";
                    var filter = Builders<BsonDocument>.Filter.Eq("useridLong", (long)doc.GetValue("useridLong"));
                    var update = Builders<BsonDocument>.Update.Set("rank", i);
                    await groupCollec.UpdateOneAsync(filter, update);
                    i++;
                }
                else
                {
                    //stored rank for rest user who werent in leaderboard
                    var filter = Builders<BsonDocument>.Filter.Eq("useridLong", (long)doc.GetValue("useridLong"));
                    var update = Builders<BsonDocument>.Update.Set("rank", i);
                    await groupCollec.UpdateOneAsync(filter, update);
                    i++;
                }
       
            }*/
            await botClient.SendTextMessageAsync(e.Message.Chat.Id, leaderBoard, parseMode: ParseMode.Html, disableWebPagePreview: true, replyToMessageId: e.Message.MessageId);
        }

        public static async Task TestAsync(Update e, IMongoCollection<BsonDocument> groupCollec, BsonDocument userData)
        {
            string testInProgress = userData.GetValue("testInProgress").AsString;
            var testedFilter = Builders<BsonDocument>.Filter.Eq("userid", e.Message.From.Id.ToString());
            DateTime testMessage = userData.GetValue("testMessage").ToUniversalTime();
            testMessage = testMessage.AddHours(4);

            if (testInProgress == "NTest")
            {
                var testTimeUpdate = Builders<BsonDocument>.Update.Set("testMessage", DateTime.Now.ToUniversalTime());
                var testInProgUpdate = Builders<BsonDocument>.Update.Set("testInProgress", "working");

                await groupCollec.UpdateOneAsync(testedFilter, testTimeUpdate);
                await groupCollec.UpdateOneAsync(testedFilter, testInProgUpdate);
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You are Tested for Coronya, Send /test again after 4 hours for test results.\nThank you for your cooperation. Stay Healthy!", replyToMessageId: e.Message.MessageId);
            }
            else if (testInProgress == "working" && testMessage.CompareTo(DateTime.Now.ToUniversalTime()) <= 0)
            {
                bool isInfected = userData.GetValue("isInfected").AsBoolean;
                if (isInfected)
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Your results has arrived!\nYou are coronya <code>POSITIVE</code>, Stay Safe! Be careful!", parseMode: ParseMode.Html, replyToMessageId: e.Message.MessageId);
                    var testedUpdate = Builders<BsonDocument>.Update.Set("tested", " + POSITIVE");
                    await groupCollec.UpdateOneAsync(testedFilter, testedUpdate);
                }
                else
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Your results has arrived!\nCongrats! You are coronya <code>NEGATIVE</code>", parseMode: ParseMode.Html, replyToMessageId: e.Message.MessageId);
                    var testedUpdate = Builders<BsonDocument>.Update.Set("tested", " - NEGATIVE");
                    await groupCollec.UpdateOneAsync(testedFilter, testedUpdate);
                }

                var testInProgUpdate = Builders<BsonDocument>.Update.Set("testInProgress", "NTest");
                await groupCollec.UpdateOneAsync(testedFilter, testInProgUpdate);
            }
            else
            {
                TimeSpan waitFor = testMessage.Subtract(DateTime.Now.ToUniversalTime());
                await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/ruka-sarashina-gif-23893688", caption: $"Your results has not yet arrived.\nPlease wait for <b>{waitFor.Hours} : {waitFor.Minutes}</b> Hours", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
            }

        }

        public static async Task betAsync(Update e, IMongoCollection<BsonDocument> groupCollec, BsonDocument userdata, FilterDefinition<BsonDocument> userFilter)
        {
            try
            {
                // bet cooldown temp disabled
                /*DateTime betuntill = userdata.GetValue("betUntill", DateTime.Now).ToUniversalTime();
                if (betuntill.CompareTo(DateTime.Now.ToUniversalTime()) <= 0)
                {
                    
                }
                else
                {
                    TimeSpan waitTime = betuntill.Subtract(DateTime.Now.ToUniversalTime());
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"We don't want many gamble addicts around here.\nWait for <code>{waitTime.Seconds} seconds</code> before using bet command again.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                }*/

                Random rand = new();
                string[] Samount = e.Message.Text.Split(' ');
                int amount;
                if (Samount.Length == 1)
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Please specify a betting amount!\nExample: /bet 100", replyToMessageId: e.Message.MessageId);
                    return;
                }
                if (int.TryParse(Samount[1], out amount))
                {
                    // DateTime betUntillExtend = DateTime.Now.ToUniversalTime();
                    // betUntillExtend = betUntillExtend.AddSeconds(30);

                    // var betUntillUpdate = Builders<BsonDocument>.Update.Set("betUntill", betUntillExtend);
                    // await groupCollec.UpdateOneAsync(userFilter, betUntillUpdate);

                    if (amount < 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Can't bet negative amount, can we?", replyToMessageId: e.Message.MessageId);
                        return;
                    }
                    if (userdata.GetValue("CoroCash").AsInt32 - amount < 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You don't have enough CoroCash to place the bet!\nCollect some CoroCash first, Looser!", replyToMessageId: e.Message.MessageId);
                        return;
                    }
                    int mainValue = rand.Next(30, 70);
                    var mainValueUpdate = Builders<BsonDocument>.Update.Set("middleNum", mainValue);
                    await groupCollec.UpdateOneAsync(userFilter, mainValueUpdate);
                    InlineKeyboardMarkup inlineKeyboard = new(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🔼"),
                                InlineKeyboardButton.WithCallbackData("🔽")
                            }
                        }

                        );

                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"Middle Number: {mainValue}\nChoose either high or Low!", replyMarkup: inlineKeyboard, replyToMessageId: e.Message.MessageId);

                    var amountFilter = Builders<BsonDocument>.Filter.Eq("userid", e.Message.From.Id.ToString());
                    var amountUpdate = Builders<BsonDocument>.Update.Set("lastBetAmount", amount);

                    await groupCollec.UpdateOneAsync(amountFilter, amountUpdate);

                }
                else
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Please specify a betting amount!\nExample: /bet 100", replyToMessageId: e.Message.MessageId);
                }
            }
            catch (Exception exp)
            {
                if (exp.Message.StartsWith("Element"))
                {
                    Console.WriteLine("error occured in catch" + exp.Message);
                    string[] array = exp.Message.Split(" ");
                    string element = array[1];
                    element = element.Remove(0, 1);
                    element = element.Remove(element.Length - 1, 1);
                    Console.WriteLine(element);
                    BsonDocument userDataNew = userdata.Add(element, DateTime.Now);
                    await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                    await betAsync(e, groupCollec, userDataNew, userFilter);
                }
            }


        }

        public static async Task CallBackQueryAsync(Update e, IMongoDatabase db)
        {
            Random rand = new Random();
            if (e.CallbackQuery.Message.Chat.Type == ChatType.Group || e.CallbackQuery.Message.Chat.Type == ChatType.Supergroup)
            {
                var groupCollec = db.GetCollection<BsonDocument>(e.CallbackQuery.Message.Chat.Id.ToString());
                var Gfilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.Message.Chat.Id.ToString());
                var userFilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.From.Id.ToString());

                var mUserFilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.Message.ReplyToMessage.From.Id.ToString());
                BsonDocument userData = await groupCollec.Find(mUserFilter).FirstAsync();
                BsonDocument userdata = await groupCollec.Find(userFilter).FirstAsync();
                int CoroCash = userdata.GetValue("CoroCash").AsInt32;

                if (e.CallbackQuery.Message.ReplyToMessage.Text.StartsWith("/bet"))
                {

                    /*await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "You have chosen " + e.CallbackQuery.Data + "\nPlease wait for results!");*/
                    await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You have chosen " + e.CallbackQuery.Data + "\nPlease wait for results!");
                    await Task.Delay(2000);


                    if (e.CallbackQuery.Data == "🔼" || e.CallbackQuery.Data == "🔽")
                    {
                        await handleBetAsync(e, groupCollec, Gfilter, userFilter, userdata, rand, CoroCash);
                    }
                }

                else if (e.CallbackQuery.Message.ReplyToMessage.Text.StartsWith("/shop"))
                {
                    int begCapacity = userdata.GetValue("bagItems").AsInt32;
                    await handleshopQuery(e, CoroCash, groupCollec, userFilter, begCapacity, userdata);
                }
                else if (e.CallbackQuery.Message.ReplyToMessage.Text.StartsWith("/rps"))
                {
                    if (e.CallbackQuery.Data == "Join🎮")
                    {
                        await HandleRPSJOINAsync(e, userData, groupCollec, CoroCash);
                    }
                    else if (e.CallbackQuery.Data == "✊" || e.CallbackQuery.Data == "✋" || e.CallbackQuery.Data == "✌️")
                    {
                        await HandleRPSAsync(e, userData, groupCollec, CoroCash);
                    }
                }
            }
        }

        public static Task handleshopQuery(Update e, int CoroCash, IMongoCollection<BsonDocument> groupCollec, FilterDefinition<BsonDocument> userFilter, int bagCapacity, BsonDocument userdata)
        {
            if (e.CallbackQuery.From.Id != e.CallbackQuery.Message.ReplyToMessage.From.Id)
            {
                botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Interference is not allowed");
                return Task.CompletedTask;
            }
            if (bagCapacity == 16)
            {
                botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You inventory is full!\nBuy a new bag or free up some space!");
                return Task.CompletedTask;
            }
            int updateAmount = 0;
            if (e.CallbackQuery.Data == "Disposable Mask😷")
            {
                if ((CoroCash - 300) < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 300;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Cotton Mask😷")
            {
                if (CoroCash - 500 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 500;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Expensive Mask😷")
            {
                updateAmount = CoroCash - 1000;
                if (CoroCash - 1000 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Vaccine 💉")
            {
                if (CoroCash - 1000 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 1000;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Expensive Vaccine 💉")
            {
                if (CoroCash - 3000 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 3000;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Light Meds💊")
            {
                if (CoroCash - 300 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 300;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Expensive Meds💊")
            {
                if (CoroCash - 1000 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 1000;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            else if (e.CallbackQuery.Data == "Heavy Meds💊")
            {
                if (CoroCash - 500 < 0)
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You dont have enough CoroCash to buy " + e.CallbackQuery.Data + ", Collect some first!");
                    return Task.CompletedTask;
                }
                updateAmount = CoroCash - 500;
                var corocashMinusFilter = Builders<BsonDocument>.Update.Set("CoroCash", updateAmount);
                var inventoryUpdate = Builders<BsonDocument>.Update.Push<String>("inventory", e.CallbackQuery.Data);
                groupCollec.UpdateOneAsync(userFilter, corocashMinusFilter);
                groupCollec.UpdateOneAsync(userFilter, inventoryUpdate);
            }
            if (e.CallbackQuery.Data == "🔙Return")
            {
                ShopAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.ReplyToMessage.MessageId, userdata);
                botClient.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                return Task.CompletedTask;
            }
            var bagItemUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", 1);
            groupCollec.UpdateOneAsync(userFilter, bagItemUpdate);

            InlineKeyboardMarkup inline = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙Return")
                    }
                }
                );
            botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, "You have successfully purchased " + e.CallbackQuery.Data + "\nCoroCash left: " + updateAmount, replyMarkup: inline);
            return Task.CompletedTask;
        }
        public static Task handleBetAsync(Update e, IMongoCollection<BsonDocument> groupCollec, FilterDefinition<BsonDocument> Gfilter, FilterDefinition<BsonDocument> userFilter, BsonDocument userdata, Random rand, int CoroCash)
        {
            if (e.CallbackQuery.From.Id != e.CallbackQuery.Message.ReplyToMessage.From.Id)
            {
                botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Interference is not allowed");
                return Task.CompletedTask;
            }
            int amount = userdata.GetValue("lastBetAmount").AsInt32;
            int mNum = userdata.GetValue("middleNum").AsInt32;
            int cNum = rand.Next(0, 100);
            double multiplier;
            while (cNum == mNum)
            {
                cNum = rand.Next(0, 100);
            }
            if (amount >= 700)
            {
                multiplier = (rand.NextDouble() * 0.7f);
            }
            else
            {
                multiplier = (rand.NextDouble() * 2.20f) + 0.5;
            }

            if (e.CallbackQuery.Data == "🔼")
            {

                if (cNum > mNum)
                {
                    amount = (int)(amount * (multiplier));
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Your decision: 🔼 \nMiddle Number : {mNum} \nChosen Number : {cNum} \n{cNum} > {mNum} - ✅ \n🎉Congrats! <b>You won the bet and earned {amount} corocash!</b>", parseMode: ParseMode.Html);
                    var betUpdate = Builders<BsonDocument>.Update.Set("CoroCash", CoroCash + amount);
                    groupCollec.UpdateOneAsync(userFilter, betUpdate);
                }
                else
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Your decision: 🔼 \nMiddle Number : {mNum} \nChosen Number : {cNum} \n{cNum} > {mNum} - ❌ \n<b>You lost the bet and {amount} CoroCash from your account!</b>", ParseMode.Html);
                    var betUpdate = Builders<BsonDocument>.Update.Set("CoroCash", CoroCash - amount);
                    groupCollec.UpdateOneAsync(userFilter, betUpdate);
                }
            }
            else if (e.CallbackQuery.Data == "🔽")
            {
                if (cNum < mNum)
                {
                    amount = (int)(amount * (multiplier));
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Your decision: 🔽 \nMiddle Number : {mNum} \nChosen Number : {cNum} \n{cNum} < {mNum} \\- ✅ \n🎉Congrats\\! **You won the bet and earned {amount} corocash\\!**", parseMode: ParseMode.MarkdownV2);
                    var betUpdate = Builders<BsonDocument>.Update.Set("CoroCash", CoroCash + amount);
                    groupCollec.UpdateOneAsync(userFilter, betUpdate);
                }
                else
                {
                    botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Your decision: 🔽 \nMiddle Number : {mNum} \nChosen Number : {cNum} \n{cNum} < {mNum} \\- ❌ \n **You lost the bet and {amount} CoroCash from your account\\!**", ParseMode.MarkdownV2);
                    var betUpdate = Builders<BsonDocument>.Update.Set("CoroCash", CoroCash - amount);
                    groupCollec.UpdateOneAsync(userFilter, betUpdate);
                }
            }
            return Task.CompletedTask;
        }

        public static async Task checkZoneAsync(Update e, BsonDocument groupData)
        {
            BsonDateTime ZoneUntil = groupData.GetValue("ZoneUntil").AsBsonDateTime;
            int coroZoneFinish = ZoneUntil.CompareTo(DateTime.Now.ToUniversalTime());
            if (coroZoneFinish <= 0)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "CoroZone is not active right now.", replyToMessageId: e.Message.MessageId);
                return;
            }
            double coroZoneMultiplier = groupData.GetValue("CoroCashMultiplier").AsDouble;
            await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"CoroZone is currently active!\nCoroCash Multiplier: {coroZoneMultiplier}%\nTime remaining: {ZoneUntil.ToUniversalTime().Subtract(DateTime.Now.ToUniversalTime()).TotalSeconds}", replyToMessageId: e.Message.MessageId);
        }
        public static async Task ShopAsync(long chatId, int messageID, BsonDocument userData)
        {
            int corocash = userData.GetValue("CoroCash").AsInt32;
            InlineKeyboardMarkup inlineKeyboard = new(
                    new[]
                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Disposable Mask😷"),
                                InlineKeyboardButton.WithCallbackData("Cotton Mask😷"),
                            },
                            new[]
                            {

                                InlineKeyboardButton.WithCallbackData("Expensive Mask😷"),
                                InlineKeyboardButton.WithCallbackData("Vaccine 💉"),

                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Expensive Vaccine 💉"),
                                InlineKeyboardButton.WithCallbackData("Light Meds💊"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Heavy Meds💊"),
                                InlineKeyboardButton.WithCallbackData("Expensive Meds💊"),
                            }


                    }

                    );
            await botClient.SendTextMessageAsync(chatId,
                $"Shop🏪\n\n🛒Disposable Mask:\nUsage - 15% infection rate decrease.\nDuration: 6 Hours\nPrice - 300💸\n\n" +
                           "🛒Cotton Mask:\nUsage - 25% infection rate decrease.\nDuration: 12 Hours\nPrice - 500💸\n\n" +
                           "🛒Expensive Mask:\nUsage - 33% infection rate decrease.\nDuration: 12 Hours\nPrice - 1000💸\n\n\n" +
                           "🛒Vaccine💉: \nUsage - Chances of dying is decreased.\nDuration: 2 Day\nPrice - 1000💸\n\n" +
                           "🛒Expensive Vaccine💉: \nUsage - Chances of dying is decreased significantly.\nDuration: 6 Day\nPrice - 3000💸\n\n\n" +
                           "🛒Light Meds💊:\nUsage - Increase Your Coronya recovery, After taking pills your recover time will be decreased by 4 hours\nInstructions: One pill every  6 hours\nPrice - 300💸\n\n" +
                           "🛒Heavy Meds💊:\nUsage - Increase Your Coronya recovery, After taking pills your recover time will be decreased by 7 hours\nInstructions: One pill every  7 hours\nPrice - 500💸\n\n" +
                           "🛒Expensive Meds💊:\nUsage - Increase Your Coronya recovery, After taking pills your recover time will be decreased by 10 hours\nInstructions: One pill every  8 hours\nPrice - 1000💸\n" +
                           "Note: Meds won't effect if you are coronya negative.\n\n\n" +
                           "👛Wallet: " + corocash + "💸\n\n\n" +
                           "👇Click any buttons below to purchase itms."
                ,
                replyMarkup: inlineKeyboard,
                replyToMessageId: messageID);
        }

        public static async Task InventoryAsync(Update e, BsonDocument userData)
        {
            BsonArray inventory = userData.GetValue("inventory").AsBsonArray;
            int CoroCash = userData.GetValue("CoroCash").AsInt32;
            int i = 1;
            string Message = "<b>Your Inventory🎒:</b>\n";
            foreach (var item in inventory)
            {
                Message = Message + "\n" + i + ". " + item.ToString();
                i++;
            }
            Message = Message + "\n\nBag Capacity: " + userData.GetValue("bagCapasity").AsInt32 + "\nWallet: " + CoroCash + "💸" + "\n\nTo use an item type : <code>/use <i>{itemname / itemNumber}</i></code>";
            await botClient.SendTextMessageAsync(e.Message.Chat.Id, Message, replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);

        }


        public static async Task UseAsync(Update e, BsonDocument userData, IMongoCollection<BsonDocument> groupCollec, FilterDefinition<BsonDocument> userFilter)
        {
            BsonArray inventory = userData.GetValue("inventory").AsBsonArray;

            string[] useString = e.Message.Text.Split(' ', 2);

            if (useString.Length == 1)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Please specify a Item to use.\n\nCurrently avilable items:\n" +
                                                                        "<code>Disposable Mask😷</code> / <code>DMask</code>" +
                                                                        "\n<code>Cotton Mask😷</code>         / <code>CMask</code> " +
                                                                        "\n<code>Expensive Mask😷</code>     / <code>EMask</code>" +
                                                                        "\n<code>Vaccine 💉</code>          / <code>Vacc</code>" +
                                                                        "\n<code>Expensive Vaccine 💉</code> / <code>EVacc</code>" +
                                                                        "\n<code>Expensive Vaccine 💉" +
                                                                        "\n<code>Light Meds💊</code> / <code>LMeds</code>" +
                                                                        "\n<code>Heavy Meds💊</code> / <code>HMeds</code>" +
                                                                        "\n<code>Expensive Meds💊</code> / <code>EMeds</code>" +
                                                                        "\n\nExample: <code>/use Disposable Mask😷</code> or <code>/use DMask</code>",
                                                                        replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                return;
            }
            int i = 0;
            bool itemFound = false;

            for (int j = 0; j <= inventory.Count; j++)
            {
                if (useString[1] == j.ToString())
                {
                    useString[1] = inventory[j - 1].ToString();
                    break;
                }
            }

            try
            {
                if (useString[1] == "Disposable Mask😷" || useString[1] == "DMask")
                {
                    useString[1] = "Disposable Mask😷";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (inventory[i] == useString[1])
                        {
                            itemFound = true;
                            inventory.RemoveAt(i);
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    DateTime maskUntill = userData.GetValue("maskUnitll").ToUniversalTime();
                    if (maskUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "A mask is currently in Use, You cant use two masks at same time!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    var maskUntillUpdate = Builders<BsonDocument>.Update.Set("maskUnitll", DateTime.Now.AddHours(6));
                    var infectionRateUpdate = Builders<BsonDocument>.Update.Set("getInfectedRate", 25);
                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, maskUntillUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, infectionRateUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You have used a Disposable Mask😷!\nInfection Rate is reduced to 25% for 6 hours.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                }
                else if (useString[1] == "Cotton Mask😷" || useString[1] == "CMask")
                {
                    useString[1] = "Cotton Mask😷";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (inventory[i] == useString[1])
                        {
                            itemFound = true;
                            inventory.RemoveAt(i);
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    DateTime maskUntill = userData.GetValue("maskUnitll").ToUniversalTime();
                    if (maskUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "A mask is currently in Use, You cant use two masks at same time!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    var maskUntillUpdate = Builders<BsonDocument>.Update.Set("maskUnitll", DateTime.Now.AddHours(12));
                    var infectionRateUpdate = Builders<BsonDocument>.Update.Set("getInfectedRate", 15);
                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, maskUntillUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, infectionRateUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You have used a Cotton Mask😷!\nInfection Rate is reduced to 15% for 12 hours.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);

                }
                else if (useString[1] == "Expensive Mask😷" || useString[1] == "EMask")
                {
                    useString[1] = "Expensive Mask😷";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (inventory[i] == useString[1])
                        {
                            itemFound = true;
                            inventory.RemoveAt(i);
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    DateTime maskUntill = userData.GetValue("maskUnitll").ToUniversalTime();
                    if (maskUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "A mask is currently in Use, You cant use two masks at same time!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    var maskUntillUpdate = Builders<BsonDocument>.Update.Set("maskUnitll", DateTime.Now.AddHours(12));
                    var infectionRateUpdate = Builders<BsonDocument>.Update.Set("getInfectedRate", 7);
                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, maskUntillUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, infectionRateUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You have used Expensive Mask😷!\nInfection Rate is reduced to 7% for 12 hours.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);

                }
                else if (useString[1] == "Vaccine 💉" || useString[1] == "Vacc")
                {
                    useString[1] = "Vaccine 💉";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (inventory[i] == useString[1])
                        {
                            itemFound = true;
                            inventory.RemoveAt(i);
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    DateTime maskUntill = userData.GetValue("vaccUntill", DateTime.Now).ToUniversalTime();
                    if (maskUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Hold on bruh, Wait for a while before you take your next vaccine dose!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    var maskUntillUpdate = Builders<BsonDocument>.Update.Set("vaccUntill", DateTime.Now.AddDays(2));
                    var infectionRateUpdate = Builders<BsonDocument>.Update.Set("dieRate", 2.69);
                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, maskUntillUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, infectionRateUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/new-game-anime-injection-aoba-gif-22551649", caption: "You have taken the Vaccine 💉!\nChances of dying is decreased for 2 days.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                }
                else if (useString[1] == "Expensive Vaccine 💉" || useString[1] == "EVacc")
                {
                    useString[1] = "Expensive Vaccine 💉";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (useString[1] == inventory[i])
                        {
                            itemFound = true;
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    inventory.RemoveAt(i);
                    DateTime vaccineUntill = userData.GetValue("vaccUntill").ToUniversalTime();
                    if (vaccineUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Hold on bruh, Wait for a while before you take your next vaccine dose!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    var maskUntillUpdate = Builders<BsonDocument>.Update.Set("vaccUntill", DateTime.Now.AddDays(6));
                    var infectionRateUpdate = Builders<BsonDocument>.Update.Set("dieRate", 0.69);
                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, maskUntillUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, infectionRateUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/new-game-anime-injection-aoba-gif-22551649", caption: "You have taken the Expensive Vaccine 💉!\nChances of dying is decreased significantly for 6 days.", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                }
                else if (useString[1] == "Light Meds💊" || useString[1] == "LMeds")
                {
                    useString[1] = "Light Meds💊";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (useString[1] == inventory[i])
                        {
                            itemFound = true;
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    inventory.RemoveAt(i);

                    DateTime medsUntill = userData.GetValue("medsUntill").ToUniversalTime();
                    TimeSpan timeSpan = medsUntill.Subtract(DateTime.Now.ToUniversalTime());
                    if (medsUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You can take your next dose of Meds after " + timeSpan.Hours + " Hours and " + timeSpan.Minutes + " Minutes", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }

                    double recoverTime = userData.GetValue("recoveryHours").AsDouble;
                    double recoveryHoursLeftNow;
                    if (recoverTime - 4 < 0)
                    {
                        recoveryHoursLeftNow = 0;
                    }
                    else
                    {
                        recoveryHoursLeftNow = recoverTime - 4;
                    }
                    var recoveryHoursUpdate = Builders<BsonDocument>.Update.Set("recoveryHours", recoveryHoursLeftNow);
                    await groupCollec.UpdateOneAsync(userFilter, recoveryHoursUpdate);

                    var medsUntillUpdate = Builders<BsonDocument>.Update.Set("medsUntill", DateTime.Now.AddHours(6));
                    await groupCollec.UpdateOneAsync(userFilter, medsUntillUpdate);

                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);

                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/pill-happy-pill-love-hearts-medicine-time-gif-17876104", caption: "You have taken your medicines. Your recovery time is reduced by 4 hours, Means you will be recovered more sooner now :)");
                }
                else if (useString[1] == "Heavy Meds💊" || useString[1] == "HMeds")
                {
                    useString[1] = "Heavy Meds💊";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (useString[1] == inventory[i])
                        {
                            itemFound = true;
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }
                    inventory.RemoveAt(i);

                    DateTime medsUntill = userData.GetValue("medsUntill").ToUniversalTime();
                    TimeSpan timeSpan = medsUntill.Subtract(DateTime.Now.ToUniversalTime());
                    if (medsUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You can take your next dose of Meds after " + timeSpan.Hours + " Hours and " + timeSpan.Minutes + " Minutes", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html); ;
                        return;
                    }

                    double recoverTime = userData.GetValue("recoveryHours").AsDouble;
                    double recoveryHoursLeftNow;
                    if (recoverTime - 7 < 0)
                    {
                        recoveryHoursLeftNow = 0;
                    }
                    else
                    {
                        recoveryHoursLeftNow = recoverTime - 7;
                    }
                    var recoveryHoursUpdate = Builders<BsonDocument>.Update.Set("recoveryHours", recoveryHoursLeftNow);
                    await groupCollec.UpdateOneAsync(userFilter, recoveryHoursUpdate);

                    var medsUntillUpdate = Builders<BsonDocument>.Update.Set("medsUntill", DateTime.Now.AddHours(7));
                    await groupCollec.UpdateOneAsync(userFilter, medsUntillUpdate);

                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);

                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/pill-happy-pill-love-hearts-medicine-time-gif-17876104", caption: "You have taken the medicines. Your recovery time is reduced by 7 hours, Means you will be recovered more sooner now :)");
                }
                else if (useString[1] == "Expensive Meds💊" || useString[1] == "EMeds")
                {
                    useString[1] = "Expensive Meds💊";
                    for (i = 0; i < inventory.Count; i++)
                    {
                        if (useString[1] == inventory[i])
                        {
                            itemFound = true;
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Wait! You dont have that item, How are you gonna use it?!", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }

                    inventory.RemoveAt(i);

                    DateTime medsUntill = userData.GetValue("medsUntill").ToUniversalTime();
                    TimeSpan timeSpan = medsUntill.Subtract(DateTime.Now.ToUniversalTime());
                    if (medsUntill.CompareTo(DateTime.Now.ToUniversalTime()) >= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You can take your next dose of Meds after " + timeSpan.Hours + " Hours and " + timeSpan.Minutes + " Minutes", replyToMessageId: e.Message.MessageId, parseMode: ParseMode.Html);
                        return;
                    }

                    double recoverTime = userData.GetValue("recoveryHours").AsDouble;
                    double recoveryHoursLeftNow;
                    if (recoverTime - 10 < 0)
                    {
                        recoveryHoursLeftNow = 0;
                    }
                    else
                    {
                        recoveryHoursLeftNow = recoverTime - 10;
                    }
                    var recoveryHoursUpdate = Builders<BsonDocument>.Update.Set("recoveryHours", recoveryHoursLeftNow);
                    await groupCollec.UpdateOneAsync(userFilter, recoveryHoursUpdate);

                    var medsUntillUpdate = Builders<BsonDocument>.Update.Set("medsUntill", DateTime.Now.AddHours(8));
                    await groupCollec.UpdateOneAsync(userFilter, medsUntillUpdate);

                    var removeFromInvUpdate = Builders<BsonDocument>.Update.Set<BsonArray>("inventory", inventory);
                    await groupCollec.UpdateOneAsync(userFilter, removeFromInvUpdate);

                    var itemCountUpdate = Builders<BsonDocument>.Update.Inc<int>("bagItems", -1);
                    await groupCollec.UpdateOneAsync(userFilter, itemCountUpdate);

                    await botClient.SendAnimationAsync(e.Message.Chat.Id, "https://tenor.com/view/pill-happy-pill-love-hearts-medicine-time-gif-17876104", caption: "You have taken the medicines. Your recovery time is reduced by 10 hours, Means you will be recovered more sooner now :) ");
                }

            }
            catch (Exception exp)
            {
                Console.WriteLine("error occured in catch" + exp.Message);
                string[] array = exp.Message.Split(" ");
                string element = array[1];
                element = element.Remove(0, 1);
                element = element.Remove(element.Length - 1, 1);
                Console.WriteLine(element);
                BsonDocument userDataNew = userData.Add(element, DateTime.Now);
                await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                await UseAsync(e, userDataNew, groupCollec, userFilter);
            }
        }

        public static async Task rpsAsync(Update e, BsonDocument userData, IMongoCollection<BsonDocument> groupCollec, FilterDefinition<BsonDocument> userFilter)
        {
            string[] messageSplitted = e.Message.Text.Split();
            if (messageSplitted.Length == 1)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                    text: "Please specify a betting amount.\nExample: /rps 100", replyToMessageId: e.Message.MessageId);
                return;
            }
            try
            {
                if (int.TryParse(messageSplitted[1], out int betAmount))
                {
                    if (betAmount <= 0)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Invalid bet amount, Bet amount must be greater than 0.", replyToMessageId: e.Message.MessageId);
                        return;
                    }
                    if (userData.GetValue("CoroCash").AsInt32 < betAmount)
                    {
                        await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You don't have enough CoroCash to place the bet", replyToMessageId: e.Message.MessageId);
                        return;
                    }
                    var betAmountUpdate = Builders<BsonDocument>.Update.Set("betAmount", betAmount);
                    await groupCollec.UpdateOneAsync(userFilter, betAmountUpdate);

                    InlineKeyboardMarkup inlinekb = new(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Join🎮")
                            }
                        }
                        );
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"[{e.Message.From.FirstName}](tg://user?id={e.Message.From.Id}) has started a Rock,Paper,Scissors game.\nBet amount: " + betAmount + "\n\nClick Button below to join 🔽", replyMarkup: inlinekb, parseMode: ParseMode.Markdown, replyToMessageId: e.Message.MessageId);
                }
                else
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                    text: "Please specify a betting amount.\nExample: /rps 100", replyToMessageId: e.Message.MessageId);
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("error occured in catch" + exp.Message);
                string[] array = exp.Message.Split(" ");
                string element = array[1];
                element = element.Remove(0, 1);
                element = element.Remove(element.Length - 1, 1);
                Console.WriteLine(element);
                BsonDocument userDataNew = userData.Add(element, 0);
                await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                await rpsAsync(e, userDataNew, groupCollec, userFilter);
            }
        }

        public static async Task HandleRPSJOINAsync(Update e, BsonDocument userData, IMongoCollection<BsonDocument> groupCollec, int CoroCash)
        {
            try
            {
                if (e.CallbackQuery.From.Id == e.CallbackQuery.Message.ReplyToMessage.From.Id)
                {
                    await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "You can't join your own game!");
                    return;
                }
                var userFilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.Message.ReplyToMessage.From.Id.ToString());
                var oppFilter = Builders<BsonDocument>.Filter.Eq("useridLong", e.CallbackQuery.From.Id);
                BsonDocument oppUserdata = await groupCollec.Find(oppFilter).FirstAsync();
                if (oppUserdata.GetValue("CoroCash").AsInt32 < userData.GetValue("betAmount").AsInt32)
                {
                    await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "You dont have enough corocash for joining this match!");
                    return;
                }
                var oppUpdate = Builders<BsonDocument>.Update.Set("rpsOpponent", e.CallbackQuery.From.Id);
                await groupCollec.UpdateOneAsync(userFilter, oppUpdate);

                InlineKeyboardMarkup inlinekb = new(
                    new[]
                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("✊"),
                                InlineKeyboardButton.WithCallbackData("✋"),
                                InlineKeyboardButton.WithCallbackData("✌️")
                            }
                    }
                    );
                await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, text: $"Rps Game is Started!\nPrize Pool: {userData.GetValue("betAmount").AsInt32 + userData.GetValue("betAmount").AsInt32}\nPlayer 1: [{e.CallbackQuery.Message.ReplyToMessage.From.FirstName}](tg://user?id={e.CallbackQuery.Message.ReplyToMessage.From.Id})\nPlayer 2: [{e.CallbackQuery.From.FirstName}](tg://user?id={e.CallbackQuery.From.Id})\n\nTurn: [{e.CallbackQuery.Message.ReplyToMessage.From.FirstName}](tg://user?id={e.CallbackQuery.Message.ReplyToMessage.From.Id})", replyMarkup: inlinekb, parseMode: ParseMode.Markdown);
                var rpsTurnUpdate = Builders<BsonDocument>.Update.Set("rpsTurn", e.CallbackQuery.Message.ReplyToMessage.From.Id);
                await groupCollec.UpdateOneAsync(userFilter, rpsTurnUpdate);
            }
            catch (Exception exp)
            {
                Console.WriteLine("error occured in catch" + exp.Message);
                string[] array = exp.Message.Split(" ");
                string element = array[1];
                element = element.Remove(0, 1);
                element = element.Remove(element.Length - 1, 1);
                Console.WriteLine(element);
                var userFilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.Message.ReplyToMessage.From.Id.ToString());
                if (element == "betAmount")
                {
                    BsonDocument userDataNew = userData.Add(element, 0);
                    await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                    await HandleRPSJOINAsync(e, userDataNew, groupCollec, CoroCash);
                }
                else if (element == "rpsOpponent")
                {
                    BsonDocument userDataNew = userData.Add(element, e.CallbackQuery.From.Id);
                    await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                    await HandleRPSJOINAsync(e, userDataNew, groupCollec, CoroCash);
                }
                else if (element == "rpsTurn")
                {
                    BsonDocument userDataNew = userData.Add(element, e.CallbackQuery.Message.ReplyToMessage.From.Id);
                    await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                    await HandleRPSJOINAsync(e, userDataNew, groupCollec, CoroCash);
                }
            }

        }

        public static async Task HandleRPSAsync(Update e, BsonDocument userData, IMongoCollection<BsonDocument> groupCollec, int CoroCash)
        {
            try
            {
                string whoWon = "none";
                int betAmount = userData.GetValue("betAmount").AsInt32;
                var userFilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.Message.ReplyToMessage.From.Id.ToString());

                if ((long)userData.GetValue("rpsTurn") != e.CallbackQuery.From.Id)
                {
                    await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "It's not your turn!");
                    return;
                }
                if ((long)userData.GetValue("rpsTurn") == e.CallbackQuery.Message.ReplyToMessage.From.Id)
                {
                    ChatMember opponent = await botClient.GetChatMemberAsync(e.CallbackQuery.Message.Chat.Id, (long)userData.GetValue("rpsOpponent"));
                    if (e.CallbackQuery.Data == "✊")
                    {
                        var rpsChoose = Builders<BsonDocument>.Update.Set("rpsChoose", "Rock");
                        await groupCollec.UpdateOneAsync(userFilter, rpsChoose);
                    }
                    else if (e.CallbackQuery.Data == "✋")
                    {
                        var rpsChoose = Builders<BsonDocument>.Update.Set("rpsChoose", "Paper");
                        await groupCollec.UpdateOneAsync(userFilter, rpsChoose);
                    }
                    else if (e.CallbackQuery.Data == "✌️")
                    {
                        var rpsChoose = Builders<BsonDocument>.Update.Set("rpsChoose", "Scissors");
                        await groupCollec.UpdateOneAsync(userFilter, rpsChoose);
                    }
                    var rpsTurn = Builders<BsonDocument>.Update.Set("rpsTurn", (long)userData.GetValue("rpsOpponent"));
                    await groupCollec.UpdateOneAsync(userFilter, rpsTurn);

                    await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"*Prize Pool: {betAmount + betAmount}*\n\n[{e.CallbackQuery.Message.ReplyToMessage.From.FirstName}](tg://user?id={e.CallbackQuery.Message.ReplyToMessage.From.Id}) has chose their option.\nTurn: [{opponent.User.FirstName}](tg://user?id={opponent.User.Id})", replyMarkup: e.CallbackQuery.Message.ReplyMarkup, parseMode: ParseMode.Markdown);
                }
                else if (userData.GetValue("rpsTurn") == userData.GetValue("rpsOpponent"))
                {
                    ChatMember opponent = await botClient.GetChatMemberAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.From.Id);
                    var oppFilter = Builders<BsonDocument>.Filter.Eq("userid", opponent.User.Id.ToString());
                    BsonDocument oppData = await groupCollec.Find(oppFilter).FirstAsync();
                    string p1 = $"[{e.CallbackQuery.Message.ReplyToMessage.From.FirstName}](tg://user?id={e.CallbackQuery.Message.ReplyToMessage.From.Id})";
                    string p2 = $"[{opponent.User.FirstName}](tg://user?id={opponent.User.Id})";
                    if (e.CallbackQuery.Data == "✊")
                    {
                        if (userData.GetValue("rpsChoose").ToString() == "Rock")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Rock✊\n{p2} : Rock✊\n\n *Draw!*\nNo transaction took place.", parseMode: ParseMode.Markdown);

                        }
                        else if (userData.GetValue("rpsChoose").ToString() == "Paper")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Paper✋\n{p2} : Rock✊\n\n{p1} *won the game* and took all the money home!", parseMode: ParseMode.Markdown);
                            whoWon = "Player 1";
                        }
                        else if (userData.GetValue("rpsChoose").ToString() == "Scissors")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Scissors✌️\n{p2} : Rock✊\n{p2} *won the game* and took all the money home!", parseMode: ParseMode.Markdown);
                            whoWon = "Player 2";
                        }
                    }
                    else if (e.CallbackQuery.Data == "✋")
                    {
                        if (userData.GetValue("rpsChoose").ToString() == "Rock")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id,
                                messageId: e.CallbackQuery.Message.MessageId,
                                $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Rock✊\n{p2} : Paper✋\n\n{p2} *won the game* and took all the money home!", parseMode: ParseMode.Markdown);
                            whoWon = "Player 2";
                        }
                        else if (userData.GetValue("rpsChoose").ToString() == "Paper")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id,
                                messageId: e.CallbackQuery.Message.MessageId,
                                $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Paper✋\n{p2} : Paper✋\n\n *Draw!*\nNo transaction took place.", parseMode: ParseMode.Markdown);
                        }
                        else if (userData.GetValue("rpsChoose").ToString() == "Scissors")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id,
                                messageId: e.CallbackQuery.Message.MessageId,
                                $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Scissors✌️\n{p2} : Paper✋\n\n{p1} *won the game* and took all the money home!", parseMode: ParseMode.Markdown);
                            whoWon = "Player 1";
                        }
                    }
                    else if (e.CallbackQuery.Data == "✌️")
                    {
                        if (userData.GetValue("rpsChoose").ToString() == "Rock")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id,
                                messageId: e.CallbackQuery.Message.MessageId,
                                $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Rock✊\n{p2} : Scissors✌️\n\n{p1} *won the game* and took all the money home!", parseMode: ParseMode.Markdown);
                            whoWon = "Player 1";
                        }
                        else if (userData.GetValue("rpsChoose").ToString() == "Paper")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id,
                                messageId: e.CallbackQuery.Message.MessageId,
                                $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Paper✋\n{p2} : Scissors✌️\n\n{p2} *won the game* and took all the money home!", parseMode: ParseMode.Markdown);
                            whoWon = "Player 2";
                        }
                        else if (userData.GetValue("rpsChoose").ToString() == "Scissors")
                        {
                            await botClient.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id,
                                messageId: e.CallbackQuery.Message.MessageId,
                                $"Prize Pool: {betAmount + betAmount}\n\n*Game Ended*\n{p1} : Scissors✌️\n{p2} : Scissors✌️\n\n *Draw!*\nNo transaction took place.", parseMode: ParseMode.Markdown);
                        }
                    }
                    if (whoWon == "Player 1")
                    {
                        var Winupdate = Builders<BsonDocument>.Update.Set("CoroCash", userData.GetValue("CoroCash").AsInt32 + betAmount);
                        var Looseupdate = Builders<BsonDocument>.Update.Set("CoroCash", oppData.GetValue("CoroCash").AsInt32 - betAmount);

                        groupCollec.UpdateOne(userFilter, Winupdate);
                        groupCollec.UpdateOne(oppFilter, Looseupdate);
                    }
                    else if (whoWon == "Player 2")
                    {
                        var Winupdate = Builders<BsonDocument>.Update.Set("CoroCash", oppData.GetValue("CoroCash").AsInt32 + betAmount);
                        var Looseupdate = Builders<BsonDocument>.Update.Set("CoroCash", userData.GetValue("CoroCash").AsInt32 - betAmount);

                        groupCollec.UpdateOne(userFilter, Winupdate);
                        groupCollec.UpdateOne(oppFilter, Looseupdate);
                    }
                }
            }
            catch (Exception exp)
            {
                var userFilter = Builders<BsonDocument>.Filter.Eq("userid", e.CallbackQuery.Message.ReplyToMessage.From.Id.ToString());

                Console.WriteLine("error occured in catch" + exp.Message);
                Console.WriteLine(exp.StackTrace);
                string[] array = exp.Message.Split(" ");
                string element = array[1];
                element = element.Remove(0, 1);
                element = element.Remove(element.Length - 1, 1);
                Console.WriteLine(element);
                if (element == "rpsTurn")
                {
                    BsonDocument userDataNew = userData.Add(element, e.CallbackQuery.From.Id);
                    await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                    await HandleRPSAsync(e, userDataNew, groupCollec, CoroCash);
                }
                else if (element == "rpsChoose")
                {
                    BsonDocument userDataNew = userData.Add(element, "none");
                    await groupCollec.ReplaceOneAsync(userFilter, userDataNew);
                    await HandleRPSAsync(e, userDataNew, groupCollec, CoroCash);
                }
            }

        }


        public static Dictionary<string, int> RedemptionCodes = new Dictionary<string, int>();
        private static readonly TelegramBotClient botClient = new TelegramBotClient(Environment.GetEnvironmentVariable("botToken"));
        static async Task Main(string[] args)
        {
            Console.Title = "CoronyaBot";
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

            var cts = new CancellationTokenSource();

            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
                ThrowPendingUpdates = true
            };


            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );



            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationtoken)
            {
                var settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable("dbConnectionString"));
                var dbclient = new MongoClient(settings);
                var db = dbclient.GetDatabase("BotDatabase");

                if (update.Type == UpdateType.CallbackQuery)
                {
                    await CallBackQueryAsync(update, db);
                    return;
                }

                if (update.Type != UpdateType.Message && update.Type != UpdateType.CallbackQuery) return;

                if (update.Type == UpdateType.Message && update.Message.Text == null) return;// checking if message type is text

                if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Supergroup)
                {
                    Console.WriteLine($"{update.Message.From.FirstName} : {update.Message.Text} in @{update.Message.Chat.Username}");
                    long useriD = update.Message.From.Id;

                    string userID = update.Message.From.Id.ToString();

                    var globalCollec = db.GetCollection<BsonDocument>("botdb");
                    var groupCollec = db.GetCollection<BsonDocument>(update.Message.Chat.Id.ToString());

                    if (!await IsCollectionExistsAsync(update.Message.Chat.Id.ToString(), dbclient)) //checking if group exist in database
                    {
                        await db.CreateCollectionAsync(update.Message.Chat.Id.ToString()); // if not add in database
                        await botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat.Id,
                                text: "Thank you for Adding me to the group. Please read /start and /help command to get started with the bot.\nEnjoy...",
                                replyToMessageId: update.Message.MessageId,
                                parseMode: ParseMode.Html
                                );

                        var thatCollec = db.GetCollection<BsonDocument>(update.Message.Chat.Id.ToString());
                        var newDoc = new BsonDocument
                            {
                                {"userid",update.Message.Chat.Id.ToString()},
                                {"isCoronaThere",false},
                                {"ZoneUntil",DateTime.Now },
                                {"isCoronaLeaked",false},
                                {"CoroCashMultiplier",0.0},
                                {"party", new BsonArray {} }
                            };
                        await thatCollec.InsertOneAsync(newDoc);
                    }


                    if (!await IsUserExistsAsync(groupCollec, userID)) // checking if user exist in group collection
                    {
                        var newDoc = new BsonDocument // if not add em in collection
                            {
                                {"userid",update.Message.From.Id.ToString()},
                                {"useridLong",update.Message.From.Id},
                                {"isUser",true},
                                {"isInfected",false},
                                {"tested","Not Yet Tested"},
                                {"testMessage",DateTime.Now},
                                {"testInProgress","NTest"},
                                {"CoroCash",200},
                                {"rank",0},
                                {"lastMessage",DateTime.Now},
                                {"dieMessage",DateTime.Now},
                                {"getInfectedRate", 40},
                                {"dieRate", 4.69},
                                {"recoveryMessage",DateTime.Now},
                                {"recoveryHours", new BsonDouble(0)},
                                {"lastBetAmount", 0},
                                {"middleNum",0 },
                                {"betUntil",DateTime.Now },
                                {"inventory", new BsonArray{ } },
                                {"bagCapasity",16},
                                {"bagItems",0 },
                                {"maskUnitll",DateTime.Now },
                                {"vaccUntill",DateTime.Now },
                                {"medsUntill",DateTime.Now }
                            };
                        await groupCollec.InsertOneAsync(newDoc);
                    }
                    if (!await IsUserExistsAsync(globalCollec, userID)) // checking if user exist in global collection
                    {
                        var newDoc = new BsonDocument
                            {
                                {"userid",update.Message.From.Id.ToString()}
                            };
                        await globalCollec.InsertOneAsync(newDoc);
                    }

                    try
                    {
                        long userid = update.Message.Chat.Id;
                        var chatID = update.Message.Chat.Id;
                        var user = update.Message.Chat.FirstName;


                        var Gfilter = Builders<BsonDocument>.Filter.Eq("userid", chatID.ToString());
                        var userFilter = Builders<BsonDocument>.Filter.Eq("userid", userID.ToString());

                        BsonDocument groupData = await groupCollec.Find(Gfilter).FirstAsync();
                        BsonDocument userData = await groupCollec.Find(userFilter).FirstAsync();

                        int CoroCash = userData.GetValue("CoroCash").ToInt32();
                        // processing each update
                        Task.Run(() => { processUpdate(update, groupCollec, groupData, userData); });

                        if (update.Message.Text == "/start" || update.Message.Text == $"/start@{me.Username}") // start Command
                        {
                            Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                            await botClient.SendTextMessageAsync(
                            chatId: chatID,
                            text: "Hello peeps! I am Coronya, Made by @veebapun. Make sure you join the chhanel for latest updates.\nRead /help next to know what bot's about.",
                            replyToMessageId: update.Message.MessageId,
                            parseMode: ParseMode.Html
                            );
                        }
                        else if (update.Message.Text == "/help" || update.Message.Text == $"/help@{me.Username}") // help command
                        {
                            Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                            await botClient.SendTextMessageAsync(
                                chatId: chatID,
                                text: $"<b>Whats the bot about?</b>\n-You are gonna leak a Coronya virus in your group and it can spread in group. Onces the virus is released the game is Started.\n\n<b>How the bot works?</b>\n- After successfully leaking the virus, Everyone in group can start earning corocash by chatting in group.\nEvery time player sends message in group, They earn corocash (One message is counted every 5 seconds to avoid spam).\nBut the catch is that the infected person or non-infected person can sneeze at any time. Once someone sneezes in group Corozone starts.\n\n<b>What happens in Corozone?</b>\n- Corocash you earn gets random multiplier while corozone lasts, Means you can earn more corocash during corozones. But be careful though, Chatting in Corozone can also get you infected.\n\n<b>Whats corocash used for?</b>\n- You can compete with friends. Flex your corocash in Leaderboard. Other than that Corocash can also be used for buying certain items from shop.\n\n<b>What happens to the infected?</b>\n- (For now)Infected person can die any time, If you die you loose all your corocash.(rip you hehe)\n\n<b>Basic Commands:</b>\n/leakerlist - (admin only)List of all leakers\n/leakvirus - (admin only)Once you are ready send this message to leak virus in your group.\n/profile - Get your group profile\n/leaderboard - Get the Group leaderboard, see whoes in top.\n/inventory - check your inventory\n/shop - do some shopping\n/use - Use an item that you bought\n/test - Test for Coronya\n\nCasino Commands:\n/bet - Place a bet using your corocash, if you win you earn some cash or else you loose bet amount\nUsage: /bet <i>bet_amount</i>\n/rps - Play Rock, Paper, Scissors with friends\nUsage: /rps <i>bet_amount</i>"
                                + "\n\n<i>Please Note that when you are leaking the virus you need atleast 4 people, But the more people there is the better. You can try waiting for few hours before leaking the virus. And check if leakerlist is filled up with people. Members are automatically added to leakerlist when they send any message</i>",
                                replyToMessageId: update.Message.MessageId,
                                parseMode: ParseMode.Html
                                );
                        }
                        else if (update.Message.Text == "/profile" || update.Message.Text == $"/profile@{me.Username}") // profile command
                        {
                            Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                            await ProfileAsync(update, groupCollec, userData);
                        }
                        else if (update.Message.Text == "/checkzone" || update.Message.Text == $"/checkzone@{me.Username}")
                        {
                            await checkZoneAsync(update, groupData);
                        }

                        else if (update.Message.Text == "/leaderboard" || update.Message.Text == $"/leaderboard@{me.Username}") // leaderboard command
                        {
                            Task.Run(() => { LeaderBoardAsync(update, groupCollec); });
                        }
                        else if (update.Message.Text == "/shop" || update.Message.Text == $"/shop@{me.Username}")
                        {
                            await ShopAsync(update.Message.Chat.Id, update.Message.MessageId, userData);
                        }
                        else if (update.Message.Text == "/inventory" || update.Message.Text == $"/inventory@{me.Username}")
                        {
                            await InventoryAsync(update, userData);
                        }
                        else if (update.Message.Text.StartsWith("/bet"))
                        {
                            await betAsync(update, groupCollec, userData, userFilter);
                        }
                        else if (update.Message.Text.StartsWith("/rps"))
                        {
                            await rpsAsync(update, userData, groupCollec, userFilter);
                        }
                        else if (update.Message.Text.StartsWith("/use"))
                        {
                            await UseAsync(update, userData, groupCollec, userFilter);
                        }
                        else if (update.Message.Text == "/test" || update.Message.Text == $"/test@{me.Username}")
                        {
                            await TestAsync(update, groupCollec, userData);
                        }

                        else if (update.Message.Text == "/leakerlist" || update.Message.Text == $"/leakerlist@{me.Username}") // leakerlist command
                        {
                            ChatMember chatMember = await botClient.GetChatMemberAsync(chatID, update.Message.From.Id);
                            if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                            {
                                Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                                await LeakersList(update, groupCollec);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                chatId: chatID,
                                text: "You need admin rights to initiate this command!",
                                replyToMessageId: update.Message.MessageId,
                                parseMode: ParseMode.Html
                                );
                            }
                        }
                        else if (update.Message.Text == "/leakvirus" || update.Message.Text == $"/leakvirus@{me.Username}") // leaking virus command
                        {
                            ChatMember chatMember = await botClient.GetChatMemberAsync(chatID, update.Message.From.Id);
                            if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                            {
                                Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                                await Release(update, groupCollec);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                chatId: chatID,
                                text: "You need admin rights to initiate this command!",
                                replyToMessageId: update.Message.MessageId,
                                parseMode: ParseMode.Html
                                );
                            }
                        }


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);

                        if (e.Message == "Object reference not set to an instance of an object.")
                        {
                            return;
                        }
                    }
                } // checking group || super group
                else if (update.Message.Chat.Type == ChatType.Private || update.Message.Chat.Type == ChatType.Channel)
                {
                    long chatID = update.Message.Chat.Id;
                    string user = update.Message.From.FirstName;
                    List<BotCommand> botCommands = new List<BotCommand>();

                    BotCommand botCommand = new BotCommand()
                    {
                        Command = "start",
                        Description = "Check if bot is alive or not"
                    };
                    BotCommand botCommand1 = new BotCommand()
                    {
                        Command = "help",
                        Description = "Get the list of command and see how bot works"
                    };
                    botCommands.Add(botCommand);
                    botCommands.Add(botCommand1);
                    BotCommandScopeAllPrivateChats @default = new();
                    await botClient.SetMyCommandsAsync(botCommands, @default);

                    if (update.Message.Text == "/start" || update.Message.Text == $"/start@{me.Username}") // start Command
                    {
                        Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                        await botClient.SendTextMessageAsync(
                        chatId: chatID,
                        text: "Hello peeps! I am Coronya, Made by @veebapun. Make sure you join the chhanel for latest updates.\nRead /help next to know what bot's about.",
                        parseMode: ParseMode.Html
                        );
                    }
                    else if (update.Message.Text == "/help" || update.Message.Text == $"/help@{me.Username}") // help command
                    {
                        Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatID} by {user}.");
                        await botClient.SendTextMessageAsync(
                                chatId: chatID,
                                text: $"<b>Whats the bot about?</b>\n-You are gonna leak a Coronya virus in your group and it can spread in group. Onces the virus is released the game is Started.\n\n<b>How the bot works?</b>\n- After successfully leaking the virus, Everyone in group can start earning corocash by chatting in group.\nEvery time player sends message in group, They earn corocash (One message is counted every 5 seconds to avoid spam).\nBut the catch is that the infected person or non-infected person can sneeze at any time. Once someone sneezes in group Corozone starts.\n\n<b>What happens in Corozone?</b>\n- Corocash you earn gets random multiplier while corozone lasts, Means you can earn more corocash during corozones. But be careful though, Chatting in Corozone can also get you infected.\n\n<b>Whats corocash used for?</b>\n- You can compete with friends. Flex your corocash in Leaderboard. Other than that Corocash can also be used for buying certain items from shop.\n\n<b>What happens to the infected?</b>\n- (For now)Infected person can die any time, If you die you loose all your corocash.(rip you hehe)\n\n<b>Basic Commands:</b>\n/leakerlist - (admin only)List of all leakers\n/leakvirus - (admin only)Once you are ready send this message to leak virus in your group.\n/profile - Get your group profile\n/leaderboard - Get the Group leaderboard, see whoes in top.\n/inventory - check your inventory\n/shop - do some shopping\n/use - Use an item that you bought\n/test - Test for Coronya\n\nCasino Commands:\n/bet - Place a bet using your corocash, if you win you earn some cash or else you loose bet amount\nUsage: /bet <i>bet_amount</i>\n/rps - Play Rock, Paper, Scissors with friends\nUsage: <code>/rps <i>betAmount</i></code>"
                                + "\n\n<i>Please Note that when you are leaking the virus you need atleast 4 people, But the more people there is the better. You can try waiting for few hours before leaking the virus. And check if leakerlist is filled up with people. Members are automatically added to leakerlist when they send any message</i>",
                                replyToMessageId: update.Message.MessageId,
                                parseMode: ParseMode.Html
                                );
                    }
                    else if (update.Message.Text.StartsWith("/addCode") && update.Message.Chat.Id == 1197998359)
                    {
                        string[] messageSplitted = update.Message.Text.Split(" ");
                        if (messageSplitted.Length == 1)
                        {
                            await botClient.SendTextMessageAsync(
                            chatId: chatID,
                            text: "b-baka!",
                            parseMode: ParseMode.Html
                            );
                            return;
                        }
                        string code = messageSplitted[1];
                        int amount = int.Parse(messageSplitted[2]);
                        await AddRedCode(db, code, amount);
                        await botClient.SendTextMessageAsync(
                        chatId: chatID,
                        text: "Successfully Circulated Cash in all Accounts!",
                        parseMode: ParseMode.Html
                        );
                    }
                    else if (update.Message.Text.StartsWith("/"))
                    {
                        await notAccess(update);
                    }

                } // checking if private


            } // Update handler

            Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellation)
            {
                Console.WriteLine("An error occured");
                Console.WriteLine(exception.Message.ToString());
                Console.WriteLine(exception.StackTrace.ToString());
                return Task.CompletedTask;
            }
            Console.WriteLine($"Start listening for @{me.Username}");
            await Task.Delay(int.MaxValue);

        } // Main ends here

        public static async Task AddRedCode(IMongoDatabase db, string code, int amount)
        {
            foreach (BsonDocument collection in db.ListCollectionsAsync().Result.ToListAsync<BsonDocument>().Result)
            {
                string name = collection["name"].AsString;
                var collec = db.GetCollection<BsonDocument>(name);

                var Filter = Builders<BsonDocument>.Filter.Eq("isUser", true);
                var Docs = await collec.Find(Filter).ToListAsync();
                foreach (var doc in Docs)
                {
                    int coroCash = doc.GetValue("CoroCash").AsInt32;
                    string userID = doc.GetValue("userid").ToString();
                    var update = Builders<BsonDocument>.Update.Set("CoroCash", coroCash + amount);
                    var filter = Builders<BsonDocument>.Filter.Eq("userid", userID);
                    await collec.UpdateOneAsync(filter, update);
                }
            }
        }
        public static async Task notAccess(Update update)
        {
            await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat.Id,
                        text: "This command is only accessible inside a group",
                        replyToMessageId: update.Message.MessageId,
                        parseMode: ParseMode.Html
                        );
        }

    } // program class ends here
}// namespace ends here
