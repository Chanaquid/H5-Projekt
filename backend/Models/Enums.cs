namespace backend.Models
{
        //Item condition
        public enum ItemCondition
        {
            Excellent = 0,  //Like new, no visible wear
            Good = 1,  //Minor signs of use, fully functional
            Fair = 2   //Visible wear or cosmetic damage, still functional
        }

        //Item status
        public enum ItemStatus
        {
            Pending = 0,  //Awaiting admin approval — not visible to borrowers
            Approved = 1,  //Live and browseable
            Rejected = 2   //Admin rejected — owner can edit and resubmit
        }

        //Loand Status
        public enum LoanStatus
        {
            //Waiting for owner to accept or decline (normal user score)
            Pending = 0,

            //Score is 20–50: Waiting for admin to approve before owner sees it
            AdminPending = 1,

            //Admin approved — borrower can now pick up the item
            Approved = 2,

            //Item has been picked up and loan is in progress
            Active = 3,

            //Item returned befire or after due date
            Returned = 4,

            //Item not returned by due date
            Late = 5,

            //Loan request was declined by admin or owner
            Rejected = 6,

            //Cancelled by borrower before pickup
            Cancelled = 7
        }

        //Fine enums
        public enum FineType
        {
            //Flat 100 kr fine for each item returning after due date
            Late = 0,

            //50% of item's current value — ruled by admin via dispute
            Damaged = 1,

            //100% of item's CurrentValue — ruled by admin via dispute
            Lost = 2,

            Custom = 3  //Admin-issued manual fine

        }

        public enum FineStatus
        {
            Unpaid = 0,
            PendingVerification = 1,  //User submitted proof, waiting for admin
            Paid = 2, //Admin approved
            Rejected = 3, //Admin rejected proof, user must resubmit
            Waived = 4
        }

        //Score enums
        public enum ScoreChangeReason
        {
            OnTimeReturn = 0,  //+5 score for every item
            LateReturn = 1,  //-5 per day up to -25 for every item
            AdminAdjustment = 2   //Manual override by admin
        }

        //dispute initiator
        public enum DisputeFiledAs
        {
            AsOwner = 0,  //"The item came back damaged/lost"
            AsBorrower = 1   //"The item was already in this condition when I got it"
        }

        //Given by admin
        public enum DisputeVerdict
        {
            OwnerFavored = 0,  //Full fine applied to borrower
            BorrowerFavored = 1,  //No fine — owner accepts pre-existing condition
            PartialDamage = 2,  //Custom fine amount set by admin
            Inconclusive = 3   //No fine, note added to both profiles
        }

        //Dispute enum
        public enum DisputeStatus
        {
            Open = 0,  //Just filed, other party not yet notified
            AwaitingResponse = 1,  //Other party has 72h to submit their evidence
            UnderReview = 2,  //Admin is reviewing both sides
            Resolved = 3   //Admin has issued a verdict
        }

        //Notif enums
        public enum NotificationType
        {
            //Loan lifecycle
            LoanRequested = 0,
            LoanApproved = 1,
            LoanRejected = 2,
            LoanCancelled = 3,
            LoanActive = 4,   //Item marked as picked up

            //Due date reminders
            DueSoon = 5,   //3 days before due date
            Overdue = 6,   //Day after due date

            //Item lifecycle
            ItemApproved = 7,
            ItemRejected = 8,

            //Fines and score
            FineIssued = 9,
            FinePaid = 10,
            ScoreChanged = 11,

            //Disputes
            DisputeFiled = 12,
            DisputeResponse = 13,  //Other party submitted their evidence
            DisputeResolved = 14,

            //Appeals
            AppealSubmitted = 15,
            AppealApproved = 16,
            AppealRejected = 17,

            //Verification
            VerificationApproved = 18,
            VerificationRejected = 19,

            //Messages
            MessageReceived = 20, //New loan chat message
            DirectMessageReceived = 21, //New direct message
            SupportMessageReceived = 22 //New message in support thread
        }

        //Type of notification
        public enum NotificationReferenceType
        {
            Loan = 0,
            Item = 1,
            Dispute = 2,
            Appeal = 3,
            Fine = 4,
            Verification = 5,
            DirectConversation = 6,
            SupportThread = 7

        }

        //Appeal status
        public enum AppealStatus
        {
                Pending = 0,
                Approved = 1,
                Rejected = 2
        }

        public enum AppealType
        {
            Score = 0,  //User wants score restored
            Fine = 1    //User wants to appeal a fine
        }

        public enum FineAppealResolution
        {
            Waive = 0, //Fine cancelled entirely
            HalfDamage = 1, //50% of item value
            FullLost = 2,//100% of item value
            Custom = 3 //Admin sets a specific amount
        }

        //Verification enums
        public enum VerificationStatus
        {
            Pending = 0,
            Approved = 1,
            Rejected = 2
        }

        public enum VerificationDocumentType
        {
            Passport = 0,
            NationalId = 1,
            DrivingLicense = 2
        }


        public enum ExtensionStatus
        {
            Pending = 0,
            Approved = 1,
            Rejected = 2
        }


        //Support thread status
        public enum SupportThreadStatus
            {
                Open = 0, //Unclaimed — visible to all admins
                Claimed = 1,//One admin owns the conversation
                Closed = 2  //Resolved — can be reopened by admin
            }

}
